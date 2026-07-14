using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace Services.TextractServices;

public class TextractJobTrackingService : ITextractJobTrackingService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly ILogger<TextractJobTrackingService> _logger;
    private const string TABLE_NAME = "TextractJobs";

    public TextractJobTrackingService(IAmazonDynamoDB dynamoClient, ILogger<TextractJobTrackingService> logger)
    {
        _dynamoClient = dynamoClient;
        _logger = logger;
    }

    public async Task SaveJobAsync(string jobId, string documentId)
    {
        var request = new PutItemRequest
        {
            TableName = TABLE_NAME,
            Item = new()
            {
                ["jobId"] = new AttributeValue { S = jobId },
                ["documentId"] = new AttributeValue { S = documentId },
                ["status"] = new AttributeValue { S = "PROCESSING" },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        };

        await _dynamoClient.PutItemAsync(request);

        _logger.LogInformation(
            "Saved Textract job tracking record. JobId: {JobId}, DocumentId: {DocumentId}",
            jobId, documentId);
    }
}