using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Core.Models;
using Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Services.DynamoDb;

public class DynamoDbService : IDynamoDbService
{
    private static readonly IAmazonDynamoDB _dynamoClient = new AmazonDynamoDBClient();
    private readonly ILogger<DynamoDbService> _logger ;
    private const string TABLE_NAME = "DocumentMetadata";
    public DynamoDbService(ILogger<DynamoDbService> logger)
    {
        _logger = logger;
    }


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

    public async Task UpdateFileStatusAsync(UpdateStatusRequest updateStatusRequest)
{
    var fileId = updateStatusRequest.DocumentId;
    var status = updateStatusRequest.Status;
    var errorMessage = updateStatusRequest.ErrorMessage;

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

        // 🔥 PRIMARY KEY
        Key = new Dictionary<string, AttributeValue>
        {
            ["documentId"] = new AttributeValue { S = fileId }
        },

        UpdateExpression = updateExpr,

        ExpressionAttributeNames = new Dictionary<string, string>
        {
            ["#status"] = "status",
            ["#updatedAt"] = "updatedAt"
        },

        ExpressionAttributeValues = values,

        ConditionExpression = "attribute_exists(documentId)"
    };

    await _dynamoClient.UpdateItemAsync(request);
}

    public async Task<List<DocumentEntity>> GetDocumentsAsync(DocumentQuery query)
    {
        if (query.StatusList == null || !query.StatusList.Any())
            return await GetAllDocumentsAsync();

        return await GetDocumentsByQuery(query);
    }

    private async Task<List<DocumentEntity>> GetAllDocumentsAsync()
    {
        var request = new ScanRequest { TableName = TABLE_NAME};

        var response = await _dynamoClient.ScanAsync(request);

       

        return MapToDocumentEntities(response.Items);
    }

    private async Task<List<DocumentEntity>> GetDocumentsByQuery(DocumentQuery query)
    {
        _logger.LogInformation("Processing query with statuses: {statuses}", string.Join(",", query.StatusList ?? []));
        var tasks = query.StatusList!
            .Select(status =>
            {
                var request = new QueryRequest
                {
                    TableName = TABLE_NAME,
                    IndexName = "status-index",
                    KeyConditionExpression = "#status = :status",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#status"] = "status"
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":status"] = new AttributeValue { S = status }
                    }
                };

                return _dynamoClient.QueryAsync(request);
            })
            .ToList();

        var responses = await Task.WhenAll(tasks);
        
        var allItems = responses.SelectMany(r => r.Items).ToList();

        _logger.LogInformation("DynamoDB returned {count} items", allItems.Count);

        return MapToDocumentEntities(allItems)
            .GroupBy(doc => doc.DocumentId)
            .Select(group => group.First())
            .ToList();
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