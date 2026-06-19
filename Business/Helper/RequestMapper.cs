
using Amazon.Lambda.APIGatewayEvents;
using Core.Models;

namespace Business.Helper;
public class RequestMapper : IRequestMapper
{
    public DocumentQuery Map(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        return new DocumentQuery
        {
            Status = request.QueryStringParameters.TryGetValue("status", out var status)
                        ? status : null     

        };
    }
}