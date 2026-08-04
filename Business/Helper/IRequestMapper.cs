using Amazon.Lambda.APIGatewayEvents;
using Core.Models;

namespace Business.Helper;

public interface IRequestMapper
{
    DocumentQuery Map(APIGatewayHttpApiV2ProxyRequest request);
}