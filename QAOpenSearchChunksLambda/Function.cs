using Amazon.Lambda.Core;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.OpenSearch;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace QAOpenSearchChunksLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly IOpenSearchSyncService _openSearchSyncService;

    public Function()
    {
        _logger = _provider.GetRequiredService<ILogger<Function>>();
        _openSearchSyncService = _provider.GetRequiredService<IOpenSearchSyncService>();
    }

    public async Task<OpenSearchQuestionChunkOutput> FunctionHandler(OpenSearchQuestionChunkInput input, ILambdaContext context)
    {
        _logger.LogInformation(
            "Searching chunks for document {DocumentId}, question: {Question}",
            input.DocumentId, input.Question);

        var chunks = await _openSearchSyncService.SearchChunksByVectorAsync(input.DocumentId, input.Vector);

        _logger.LogInformation("Retrieved {Count} chunk(s)", chunks.Count);

        return new OpenSearchQuestionChunkOutput
        {
            Question = input.Question,
            Chunks = chunks
        };
    }
}

