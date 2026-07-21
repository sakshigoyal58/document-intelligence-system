using Amazon.Lambda.Core;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.EmbeddingServices;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace QAEmbedQuestionLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly IEmbeddingService _embeddingService;

    public Function()
    {
        _logger = _provider.GetRequiredService<ILogger<Function>>();
        _embeddingService = _provider.GetRequiredService<IEmbeddingService>();
    }

    public async Task<EmbedQuestionOutputEntity> FunctionHandler(QuestionInputEntity input, ILambdaContext context)
    {
        _logger.LogInformation(
            "Embedding question for document {DocumentId}: {Question}",
            input.DocumentId, input.Question);

        var vector = await _embeddingService.GenerateEmbeddingAsync(input.Question);

        _logger.LogInformation("Generated question embedding. Vector length: {Length}", vector.Length);

        return new EmbedQuestionOutputEntity
        {
            DocumentId = input.DocumentId,
            Question = input.Question,
            Vector = vector
        };
    }
}
