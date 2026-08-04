using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Business.Validation;
using Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;
using Core.Constants;
using Core.DTOs;
using Core.Helpers;
using static Amazon.Lambda.S3Events.S3Event;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace ProcessingLambda;

public class Function
{
    // -----------------------------
    // Shared DI container (cold start)
    // -----------------------------
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    // -----------------------------
    // Dependencies
    // -----------------------------
    private readonly IDynamoDbService _dynamoDBService;
    private readonly IPdfValidator _validator;
    private readonly ILogger<Function> _logger;

    // -----------------------------
    // Constructor
    // -----------------------------
    public Function()
    {
        _dynamoDBService =
            _provider.GetRequiredService<IDynamoDbService>();

        _validator =
            _provider.GetRequiredService<IPdfValidator>();

        _logger =
            _provider.GetRequiredService<ILogger<Function>>();
    }

    // -----------------------------
    // Handler
    // -----------------------------
    public async Task FunctionHandler(
        S3Event s3Event,
        ILambdaContext context)
    {
        try
        {
            foreach (var record in s3Event.Records)
            {
                try
                {
                    await ProcessFile(record);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process S3 event record for key {S3Key}", record.S3.Object.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing S3 event");
            throw;
        }
    }

    // -----------------------------
    // Core logic
    // -----------------------------
    private async Task ProcessFile(
        S3EventNotificationRecord record)
    {
        var s3Key = record.S3.Object.Key;
        var fileSize = record.S3.Object.Size;

        if (string.IsNullOrWhiteSpace(s3Key))
        {
            throw new InvalidOperationException("S3 object key is missing.");
        }

        var (documentId, fileName) = DocumentStorageKey.Parse(s3Key);

        _logger.LogInformation(
            "Processing file {FileName} ({FileSize} bytes) for document {DocumentId}",
            fileName, fileSize, documentId);

        var validation = _validator.Validate(fileName, fileSize);

        if (validation.IsValid)
        {
            await _dynamoDBService.UpdateFileStatusAsync(new UpdateStatusRequest
            {
                DocumentId = documentId,
                Status = DocumentStatus.Validated
            });

            _logger.LogInformation("Validated {DocumentId}", documentId);
        }
        else
        {
            await _dynamoDBService.UpdateFileStatusAsync(new UpdateStatusRequest
            {
                DocumentId = documentId,
                Status = DocumentStatus.Invalidated,
                ErrorMessage = validation.Error
            });

            _logger.LogWarning("Validation failed for {DocumentId}: {Error}", documentId, validation.Error);
        }
    }
}