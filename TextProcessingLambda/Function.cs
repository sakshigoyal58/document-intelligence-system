using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.DocumentTextExtractionAndProcessingService;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TextProcessingLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;

    private readonly IDocumentTextProcessingService _documentTextProcessingService;

    public Function()
    {

        _logger =
            _provider.GetRequiredService<ILogger<Function>>();
        _documentTextProcessingService =
            _provider.GetRequiredService<IDocumentTextProcessingService>();
    }
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        try
        {
            _logger.LogInformation(
                "TextProcessingLambda received request: {Request}",
                JsonSerializer.Serialize(dynamoEvent));

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

                _logger.LogInformation("Processing document {DocumentId} with key {S3Key}", documentId, s3Key);

                await _documentTextProcessingService.ProcessTextFromDocumentAsync(documentId, s3Key);

                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DynamoDB event.");
        }
    }

}
