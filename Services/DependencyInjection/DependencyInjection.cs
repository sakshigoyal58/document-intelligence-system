using Microsoft.Extensions.DependencyInjection;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;
using Business.Helper;

namespace Services.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddLambdaLogger();
        });

        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IDynamoDbService, DynamoDbService>();

        services.AddSingleton<IRequestMapper, RequestMapper>();

        return services.BuildServiceProvider();
    }
}
