using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Business.Helper;
using Services.DependencyInjection;
using Services.DynamoDb;
using Core.DTOs;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace ReviewLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly IDynamoDbService _documentDBService;
    private readonly IRequestMapper _requestMapper;
    private readonly ILogger<Function> _logger;

    public Function()
    {
        _documentDBService =
            _provider.GetRequiredService<IDynamoDbService>();

        _requestMapper =
            _provider.GetRequiredService<IRequestMapper>();

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
                "ReviewLambda received request: {Request}",
                JsonSerializer.Serialize(request));

            
            request.PathParameters ??= new Dictionary<string, string>();
            request.PathParameters.TryGetValue("documentId", out var documentId);
            _logger.LogInformation(
                "ReviewLambda processing documentId: {DocumentId}",
                documentId);

            var json = JsonSerializer.Deserialize<JsonElement>(request.Body);

            var status = json.GetProperty("status").GetString();

            _logger.LogInformation(
                "ReviewLambda processing status: {Status}", status);

            if (string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(status))
            {
                _logger.LogWarning("ReviewLambda received invalid payload or missing path parameter");

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { message = "documentId (path) and status (body) are required" }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    }
                };
            }

            await _documentDBService.UpdateFileStatusAsync(new Core.DTOs.UpdateStatusRequest
            {
                DocumentId = documentId,
                Status = status
            });

            _logger.LogInformation("ReviewLambda updated document status: {DocumentId} -> {Status}", documentId, status);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { message = "Status updated" }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
        catch (Exception ex)
{
    _logger.LogError(ex, "ReviewLambda failed");

    return new APIGatewayHttpApiV2ProxyResponse
    {
        StatusCode = 500,
        Body = JsonSerializer.Serialize(new
        {
            message = ex.Message,
            stack = ex.StackTrace
        })
    };
}
    }
}
