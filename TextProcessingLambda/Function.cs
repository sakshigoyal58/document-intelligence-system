using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.DocumentTextExtractionAndProcessingService;
using Services.TextractServices;
using System.Text.Json;
using Core.Constants;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TextProcessingLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly ITextractService _textractService;
    private readonly ITextractJobTrackingService _textractJobTrackingService;

    public Function()
    {

        _logger =
            _provider.GetRequiredService<ILogger<Function>>();
        _textractService =
            _provider.GetRequiredService<ITextractService>();
        _textractJobTrackingService =
            _provider.GetRequiredService<ITextractJobTrackingService>();
    }
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        _logger.LogInformation("TextExtractionLambda received {Count} records", dynamoEvent.Records.Count);

        foreach (var record in dynamoEvent.Records)
        {
            var eventName = record.EventName ?? string.Empty;
            var isUpdateEvent = string.Equals(eventName, "MODIFY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventName, "UPDATE", StringComparison.OrdinalIgnoreCase);

            if (!isUpdateEvent)
            {
                _logger.LogInformation("Skipping TextProcessingLambda event {EventName}", eventName);
                continue;
            }

            var newImage = record.Dynamodb?.NewImage;
            if (newImage == null)
            {
                _logger.LogWarning("Skipping TextProcessingLambda record because the DynamoDB stream image is missing for event {EventName}", eventName);
                continue;
            }

            if (!newImage.TryGetValue("documentId", out var documentIdAttr) ||
                !newImage.TryGetValue("s3Key", out var s3KeyAttr) ||
                string.IsNullOrWhiteSpace(documentIdAttr.S) ||
                string.IsNullOrWhiteSpace(s3KeyAttr.S))
            {
                _logger.LogWarning("Skipping record — missing documentId or s3Key");
                continue;
            }

            var documentId = documentIdAttr.S;
            var s3Key = s3KeyAttr.S;
            var status = newImage.TryGetValue("status", out var statusAttr) ? statusAttr.S : string.Empty;
            var shouldProcess = string.Equals(status, DocumentStatus.Validated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, DocumentStatus.Invalidated, StringComparison.OrdinalIgnoreCase);

            if (!shouldProcess)
            {
                _logger.LogInformation(
                    "Skipping Textract start for document {DocumentId} because status is {Status} and event is {EventName}",
                    documentId,
                    status,
                    eventName);
                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Starting Textract job for document {DocumentId} with key {S3Key} after status {Status}",
                    documentId,
                    s3Key,
                    status);

                var jobId = await _textractService.StartTextDetectionJobAsync(s3Key);
                await _textractJobTrackingService.SaveJobAsync(jobId, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to start Textract job for document {DocumentId} with key {S3Key}. Error: {ErrorMessage}. Details: {ErrorDetails}",
                    documentId, s3Key, ex.Message, ex.ToString());
            }
        }
    }

}
