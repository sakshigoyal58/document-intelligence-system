using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Business.Helper;
using Services.DynamoDb;

public class Function
{
    private readonly  IDynamoDbService _documentDBService;
    private readonly IRequestMapper _requestMapper ;

    public Function()
        : this(new DynamoDbService(), new RequestMapper())
    {
    }

    public Function(IDynamoDbService documentDBService, IRequestMapper requestMapper)
    {
        _documentDBService = documentDBService;
        _requestMapper = requestMapper;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        var query = _requestMapper.Map(request);

        var documents = await _documentDBService.GetDocumentsAsync(query);

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
}