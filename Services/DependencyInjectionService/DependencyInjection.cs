using Microsoft.Extensions.DependencyInjection;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;
using Business.Helper;
using Services.OpenSearch;
using Microsoft.Extensions.Configuration;
using Core.Models;
using Business.Validation;

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

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.Configure<OpenSearchSetting>(
            configuration.GetSection("OpenSearch"));

        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IDynamoDbService, DynamoDbService>();
        services.AddSingleton<IRequestMapper, RequestMapper>();
        services.AddSingleton<IPdfValidator, PdfValidator>();
        services.AddHttpClient<IOpenSearchSyncService, OpenSearchSyncService>()
    .ConfigurePrimaryHttpMessageHandler(() => SigV4HandlerFactory.Create());

        return services.BuildServiceProvider();
    }
}
