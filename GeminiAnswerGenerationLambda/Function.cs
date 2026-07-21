using Amazon.Lambda.Core;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Services.AIService;
using Services.DependencyInjection;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GeminiAnswerGenerationLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly IGeminiAnswerGenerationService _answerGenerationService;

    public Function()
    {
        _logger = _provider.GetRequiredService<ILogger<Function>>();
        _answerGenerationService = _provider.GetRequiredService<IGeminiAnswerGenerationService>();
    }

    public async Task<GeminiQABuildOutputEntity> FunctionHandler(GeminiQABuildInputEntity input, ILambdaContext context)
    {
        _logger.LogInformation("Generating answer for question: {Question}", input.Question);

        var answer = await _answerGenerationService.GenerateAnswerAsync(input.Question, input.Chunks);

        return new GeminiQABuildOutputEntity { Answer = answer };
    }
}
