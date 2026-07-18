using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.DocumentTextExtractionAndProcessingService;
using Services.TextractServices;
using System.Text.Json;

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
        if (record.EventName != "INSERT")
            continue;

        var newImage = record.Dynamodb.NewImage;

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

        try
        {
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
