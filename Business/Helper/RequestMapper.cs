
using Amazon.Lambda.APIGatewayEvents;
using Core.Models;

namespace Business.Helper;
public class RequestMapper : IRequestMapper
{
    public DocumentQuery Map(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        request.QueryStringParameters ??= new Dictionary<string, string>();

        request.QueryStringParameters.TryGetValue("status", out var status);

        return new DocumentQuery
        {
            StatusList = string.IsNullOrWhiteSpace(status)? null
                        : status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim()).ToList()
        };
    }
}