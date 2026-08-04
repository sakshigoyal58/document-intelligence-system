using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Core.Constants;
using Core.Models;
using Core.DTOs;
using Microsoft.Extensions.Logging;

namespace Services.DynamoDb;

public class DynamoDbService : IDynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly ILogger<DynamoDbService> _logger;
    private const string TABLE_NAME = "DocumentMetadata";

    public DynamoDbService(IAmazonDynamoDB dynamoClient, ILogger<DynamoDbService> logger)
    {
        _dynamoClient = dynamoClient;
        _logger = logger;
    }

    public async Task AddFileRecordAsync(string fileId, string fileName, long fileSize, string s3Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        await UpsertFileRecordAsync(new UpdateStatusRequest
        {
            DocumentId = fileId,
            FileName = fileName,
            FileSize = fileSize,
            S3Key = s3Key,
            Status = DocumentStatus.Uploading
        });
    }

    public async Task UpsertFileRecordAsync(UpdateStatusRequest updateStatusRequest)
    {
        ArgumentNullException.ThrowIfNull(updateStatusRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateStatusRequest.DocumentId);

        var now = DateTime.UtcNow.ToString("O");
        var item = new Dictionary<string, AttributeValue>
        {
            ["documentId"] = new AttributeValue { S = updateStatusRequest.DocumentId },
            ["status"] = new AttributeValue { S = updateStatusRequest.Status },
            ["updatedAt"] = new AttributeValue { S = now }
        };

        if (!string.IsNullOrWhiteSpace(updateStatusRequest.FileName))
        {
            item["fileName"] = new AttributeValue { S = updateStatusRequest.FileName };
        }

        if (updateStatusRequest.FileSize.HasValue)
        {
            item["fileSize"] = new AttributeValue { N = updateStatusRequest.FileSize.Value.ToString() };
        }

        if (!string.IsNullOrWhiteSpace(updateStatusRequest.S3Key))
        {
            item["s3Key"] = new AttributeValue { S = updateStatusRequest.S3Key };
        }

        if (!string.IsNullOrWhiteSpace(updateStatusRequest.ErrorMessage))
        {
            item["ErrorMessage"] = new AttributeValue { S = updateStatusRequest.ErrorMessage };
        }

        if (!item.ContainsKey("createdAt"))
        {
            item["createdAt"] = new AttributeValue { S = now };
        }

        var request = new PutItemRequest
        {
            TableName = TABLE_NAME,
            Item = item
        };

        await _dynamoClient.PutItemAsync(request);
    }

    public async Task UpdateFileStatusAsync(UpdateStatusRequest updateStatusRequest)
    {
        ArgumentNullException.ThrowIfNull(updateStatusRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateStatusRequest.DocumentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateStatusRequest.Status);

        _logger.LogInformation(
            "Updating document status: {DocumentId} -> {Status}",
            updateStatusRequest.DocumentId,
            updateStatusRequest.Status);

        var values = new Dictionary<string, AttributeValue>
        {
            [":status"] = new AttributeValue { S = updateStatusRequest.Status },
            [":ts"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(updateStatusRequest.ErrorMessage))
        {
            values[":err"] = new AttributeValue { S = updateStatusRequest.ErrorMessage };
        }

        var updateExpr = !string.IsNullOrWhiteSpace(updateStatusRequest.ErrorMessage)
            ? "SET #status = :status, #updatedAt = :ts, ErrorMessage = :err"
            : "SET #status = :status, #updatedAt = :ts";

        var request = new UpdateItemRequest
        {
            TableName = TABLE_NAME,
            Key = new Dictionary<string, AttributeValue>
            {
                ["documentId"] = new AttributeValue { S = updateStatusRequest.DocumentId }
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
                    FileName = GetStringValue(item, "fileName", "untitled.pdf"),
                    FileStatus = GetStringValue(item, "status", "UNKNOWN"),
                    CreatedAt = GetDateTimeValue(item, "createdAt", DateTime.UtcNow),
                    UpdatedAt = GetDateTimeValue(item, "updatedAt", DateTime.UtcNow)
                };
            })
            .Where(doc => doc != null)
            .ToList();
    }

    private static string GetStringValue(Dictionary<string, AttributeValue> item, string key, string fallback = "")
    {
        return item.TryGetValue(key, out var value) && value?.S != null
            ? value.S
            : fallback;
    }

    private static DateTime GetDateTimeValue(Dictionary<string, AttributeValue> item, string key, DateTime fallback)
    {
        if (item.TryGetValue(key, out var value) && value?.S != null && DateTime.TryParse(value.S, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

}