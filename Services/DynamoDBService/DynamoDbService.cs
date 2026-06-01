using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Services.DynamoDb;

public class DynamoDbFileService : IDynamoDbFileService
{
    private static readonly IAmazonDynamoDB _dynamoClient = new AmazonDynamoDBClient();
    private const string TABLE_NAME = "DocumentMetadata";

    public async Task AddFileRecordAsync(string fileId, string fileName, long fileSize)
{
    var request = new PutItemRequest
    {
        TableName = TABLE_NAME,
        Item = new()
        {
            ["documentId"] = new AttributeValue { S = fileId },
            ["fileName"] = new AttributeValue { S = fileName },
            ["fileSize"] = new AttributeValue { N = fileSize.ToString() },
            ["status"] = new AttributeValue { S = "FILE_UPLOADED" },
            ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
        }
    };

    await _dynamoClient.PutItemAsync(request);
}

    public async Task UpdateFileStatusAsync(string fileId, string status, string? errorMessage = null)
{
    var values = new Dictionary<string, AttributeValue>
    {
        [":status"] = new AttributeValue { S = status },
        [":ts"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
    };

    if (errorMessage != null)
        values[":err"] = new AttributeValue { S = errorMessage };

    var updateExpr = errorMessage != null
        ? "SET #status = :status, #updatedAt = :ts, ErrorMessage = :err"
        : "SET #status = :status, #updatedAt = :ts";

    var request = new UpdateItemRequest
    {
        TableName = TABLE_NAME,
        Key = new() { ["documentId"] = new AttributeValue { S = fileId } },
        UpdateExpression = updateExpr,
        ExpressionAttributeNames = new() 
        { 
            ["#status"] = "Status",
            ["#updatedAt"] = "UpdatedAt"  // ← ADD THIS LINE
        },
        ExpressionAttributeValues = values
    };

    await _dynamoClient.UpdateItemAsync(request);
}
}