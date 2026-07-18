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

    public async Task<string?> GetDocumentIdAsync(string jobId)
    {
        var request = new GetItemRequest
        {
            TableName = TABLE_NAME,
            Key = new()
            {
                ["jobId"] = new AttributeValue { S = jobId }
            }
        };

        var response = await _dynamoClient.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            _logger.LogWarning("No TextractJobs record found for JobId: {JobId}", jobId);
            return null;
        }

        if (!response.Item.TryGetValue("documentId", out var documentIdAttr))
        {
            _logger.LogWarning("TextractJobs record for JobId {JobId} missing documentId", jobId);
            return null;
        }

        return documentIdAttr.S;
    }

    public async Task UpdateStatusAsync(string jobId, string status)
    {
        var request = new UpdateItemRequest
        {
            TableName = TABLE_NAME,
            Key = new()
            {
                ["jobId"] = new AttributeValue { S = jobId }
            },
            UpdateExpression = "SET #s = :status, updatedAt = :updatedAt",
            ExpressionAttributeNames = new()
            {
                ["#s"] = "status" // "status" can be a reserved word in DynamoDB expressions, so we alias it
            },
            ExpressionAttributeValues = new()
            {
                [":status"] = new AttributeValue { S = status },
                [":updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
            }
        };

        await _dynamoClient.UpdateItemAsync(request);

        _logger.LogInformation("Updated TextractJobs status to {Status} for JobId {JobId}", status, jobId);
    }

    public async Task<string?> GetStatusAsync(string jobId)
    {
        var request = new GetItemRequest
        {
            TableName = TABLE_NAME,
            Key = new()
            {
                ["jobId"] = new AttributeValue { S = jobId }
            }
        };

        var response = await _dynamoClient.GetItemAsync(request);

        if (response.Item == null || !response.Item.TryGetValue("status", out var statusAttr))
        {
            return null;
        }

        return statusAttr.S;
    }
}