using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Business.Helper;
using Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer)
)]

namespace QueryLambda;

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
                "Received request: {Request}",
                JsonSerializer.Serialize(request));

            var query = _requestMapper.Map(request);

            _logger.LogInformation(
                "Mapped statuses: {Statuses}",
                string.Join(",", query.StatusList ?? []));

            var documents =
                await _documentDBService.GetDocumentsAsync(query);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(documents),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");

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