using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.OpenSearch;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace SearchTextLambda;

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

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            _logger.LogInformation(
                "Received search request: {Request}",
                JsonSerializer.Serialize(request));

            // Extract searchText from query parameters
            string? searchText = null;
            if (request.QueryStringParameters != null &&
                request.QueryStringParameters.TryGetValue("searchText", out var searchValue))
            {
                searchText = searchValue;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _logger.LogWarning("searchText query parameter is missing or empty");

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new
                    {
                        message = "searchText query parameter is required"
                    }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }

            _logger.LogInformation("Searching for documents with text: {SearchText}", searchText);

            // Call OpenSearch service
            var results = await _openSearchService.SearchDocumentsByNameAsync(searchText);

            _logger.LogInformation("Found {Count} documents matching search text: {SearchText}", results.Count, searchText);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(results),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request");

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new
                {
                    message = "Internal Server Error"
                }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
    }
}
