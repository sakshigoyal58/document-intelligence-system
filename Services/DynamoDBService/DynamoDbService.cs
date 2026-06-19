using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Core.Models;

namespace Services.DynamoDb;

public class DynamoDbService : IDynamoDbService
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
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
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
            ["#status"] = "status",
            ["#updatedAt"] = "updatedAt"  // ← ADD THIS LINE
        },
        ExpressionAttributeValues = values
    };

    await _dynamoClient.UpdateItemAsync(request);
    }

    public async Task<List<DocumentEntity>> GetDocumentsAsync(DocumentQuery query)
    {
        if (string.IsNullOrEmpty(query.Status))
        {
            return await GetAllDocumentsAsync();
        }
        else
        {
            return await QueryAsync(query);
        }
    }

    private async Task<List<DocumentEntity>> GetAllDocumentsAsync()
    {
        var request = new ScanRequest { TableName = TABLE_NAME};

        var response = await _dynamoClient.ScanAsync(request);

        return MapToDocumentEntities(response.Items);
    }

    private async Task<List<DocumentEntity>> QueryAsync(DocumentQuery query)
    {
        var request = new QueryRequest
        {
            TableName = TABLE_NAME,
            IndexName = "GSI1",
            KeyConditionExpression = "#status = :status",

            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status"
            },

            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = query.Status }
            }
        };

        var response = await _dynamoClient.QueryAsync(request);

        return MapToDocumentEntities(response.Items);
    }

    private static List<DocumentEntity> MapToDocumentEntities(List<Dictionary<string, AttributeValue>> items)
    {
        return items
            .Where(item => item != null && item.Count > 0)
            .Select(item =>
            {
                    return new DocumentEntity
                    {
                        DocumentId = GetStringValue(item, "documentId"),
                        FileName = GetStringValue(item, "fileName"),
                        FileStatus = GetStringValue(item, "status"),
                        CreatedAt = DateTime.Parse(GetStringValue(item, "createdAt")),
                        UpdatedAt = DateTime.Parse(GetStringValue(item, "updatedAt"))
                    };
            })
            .Where(doc => doc != null)
            .ToList();
    }

     private static string GetStringValue(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) && value?.S != null
            ? value.S
            : throw new KeyNotFoundException($"Required field '{key}' not found in DynamoDB item");
    } 

}