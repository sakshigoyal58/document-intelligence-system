using System.Net.Http;
using AwsSignatureVersion4;
using Amazon.Runtime;

namespace Services.OpenSearch;

public static class SigV4HandlerFactory
{
    public static DelegatingHandler Create()
    {
        return new AwsSignatureHandler(
            new AwsSignatureHandlerSettings(
                "us-east-1",
                "es",              // service name for OpenSearch
                FallbackCredentialsFactory.GetCredentials() // uses Lambda execution role automatically
            )
        )
        {
            InnerHandler = new HttpClientHandler()
        };
    }
}