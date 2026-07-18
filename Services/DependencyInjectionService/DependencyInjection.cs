using Microsoft.Extensions.DependencyInjection;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.Textract;
using Microsoft.Extensions.Logging;
using Services.DynamoDb;
using Business.Helper;
using Services.OpenSearch;
using Microsoft.Extensions.Configuration;
using Core.Models;
using Business.Validation;
using Services.S3Service;
using Services.DocumentTextExtractionAndProcessingService;
using Services.TextractServices;
using Amazon.BedrockRuntime;
using Services.EmbeddingServices;
using Services.TextChunkingServices;

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

        services.AddAWSService<IAmazonDynamoDB>();
        services.AddSingleton<IDynamoDbService, DynamoDbService>();
        services.AddSingleton<IRequestMapper, RequestMapper>();
        services.AddSingleton<IPdfValidator, PdfValidator>();
        services.AddHttpClient<IOpenSearchSyncService, OpenSearchSyncService>()
            .ConfigurePrimaryHttpMessageHandler(() => SigV4HandlerFactory.Create());

        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonTextract>();
        services.AddSingleton<ITextractService, TextractService>();
        services.AddSingleton<ITextractJobTrackingService, TextractJobTrackingService>();
        services.AddSingleton<IS3Service, s3Service>();
        services.AddSingleton<IDocumentTextProcessingService, DocumentTextProcessingService>();
        services.AddAWSService<IAmazonBedrockRuntime>();
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<ITextChunkingService, TextChunkingService>();

        return services.BuildServiceProvider();
    }
}