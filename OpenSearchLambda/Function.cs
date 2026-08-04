using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.OpenSearch;
using Core.Constants;
using Core.Models;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace OpenSearchLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly IOpenSearchSyncService _openSearchService;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        _openSearchService =
            _provider.GetRequiredService<IOpenSearchSyncService>();

        _logger =
            _provider.GetRequiredService<ILogger<Function>>();
    }

    public async Task FunctionHandler(DynamoDBEvent dynamoEvent)
    {
        try
        {
            _logger.LogInformation(
                "OpenSearchLambda received request: {Request}",
                JsonSerializer.Serialize(dynamoEvent));

            foreach (var record in dynamoEvent.Records)
            {
                var eventName = record.EventName ?? string.Empty;
                var isUpdateEvent = string.Equals(eventName, "MODIFY", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(eventName, "UPDATE", StringComparison.OrdinalIgnoreCase);

                if (!isUpdateEvent)
                {
                    _logger.LogInformation("Skipping OpenSearch indexing for event {EventName}", eventName);
                    continue;
                }

                var newImage = record.Dynamodb?.NewImage;
                if (newImage == null)
                {
                    _logger.LogWarning("Skipping OpenSearch indexing because the DynamoDB stream image is missing for event {EventName}", eventName);
                    continue;
                }

                var documentId = newImage.TryGetValue("documentId", out var documentIdAttr) ? documentIdAttr.S : string.Empty;
                var status = newImage.TryGetValue("status", out var statusAttr) ? statusAttr.S : string.Empty;
                var isRelevantStatus = string.Equals(status, DocumentStatus.Validated, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, DocumentStatus.Invalidated, StringComparison.OrdinalIgnoreCase);

                if (!isRelevantStatus)
                {
                    _logger.LogInformation(
                        "Skipping OpenSearch indexing for {DocumentId} because status is {Status} and event is {EventName}",
                        documentId,
                        status,
                        eventName);
                    continue;
                }

                var payload = new OpenSearchDocumentPayload
                {
                    DocumentId = documentId,
                    FileName = newImage.TryGetValue("fileName", out var fileNameAttr) && !string.IsNullOrWhiteSpace(fileNameAttr.S)
                        ? fileNameAttr.S
                        : "untitled.pdf"
                };

                _logger.LogInformation(
                    "OpenSearchLambda processing documentId: {DocumentId} for event {EventName} with status {Status}",
                    payload.DocumentId,
                    eventName,
                    status);

                await _openSearchService.IndexDocumentAsync(payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing DynamoDB event: {Event}",
                JsonSerializer.Serialize(dynamoEvent));
            throw;
        }
    }
}
