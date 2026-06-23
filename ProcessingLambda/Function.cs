using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Business.Validation;
using Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;
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
                await ProcessFile(record);
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
        var fileName = record.S3.Object.Key;
        var fileSize = record.S3.Object.Size;
        var fileId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Processing file {FileName} ({FileSize} bytes)",
            fileName, fileSize);

        await _dynamoDBService.AddFileRecordAsync(
            fileId, fileName, fileSize);

        _logger.LogInformation("Record created {FileId}", fileId);

        var validation = _validator.Validate(fileName, fileSize);

        if (validation.IsValid)
        {
            await _dynamoDBService.UpdateFileStatusAsync(
                fileId, "VALIDATED");

            _logger.LogInformation("Validated {FileId}", fileId);
        }
        else
        {
            await _dynamoDBService.UpdateFileStatusAsync(
                fileId,
                "VALIDATION_FAILED",
                validation.Error);

            _logger.LogWarning(
                "Validation failed {Error}", validation.Error);
        }
    }
}