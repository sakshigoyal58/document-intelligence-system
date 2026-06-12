using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Services.DynamoDb;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace QueryLambda;

public class Function
{
     private readonly IDynamoDbFileService _fileService;

     public Function()
    : this(new DynamoDbFileService())
    {} 

    public Function(IDynamoDbFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine("Fetching documents from DynamoDB...");

        var documents = await _fileService.GetAllDocumentsAsync();

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
