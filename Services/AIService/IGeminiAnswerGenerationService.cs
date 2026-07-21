namespace Services.AIService;

public interface IGeminiAnswerGenerationService
{
    Task<string> GenerateAnswerAsync(string question, List<string> chunks);
}