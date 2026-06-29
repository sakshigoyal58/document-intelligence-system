using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.OpenSearch;
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
                // ONLY INSERT events
                if (record.EventName != "INSERT")
                    continue;

                var newImage = record.Dynamodb.NewImage;

                var payload = new OpenSearchDocumentPayload
                {
                    DocumentId = newImage["documentId"].S,
                    FileName = newImage["documentName"].S
                };

                _logger.LogInformation(
                    "OpenSearchLambda processing documentId: {DocumentId}",
                    payload.DocumentId);

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
