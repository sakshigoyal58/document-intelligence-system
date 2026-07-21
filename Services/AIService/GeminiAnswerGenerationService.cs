using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Services.AIService;

public class GeminiAnswerGenerationService : IGeminiAnswerGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAnswerGenerationService> _logger;
    private readonly string _apiKey;
    private const string ModelName = "gemini-3.5-flash";

    public GeminiAnswerGenerationService(HttpClient httpClient, ILogger<GeminiAnswerGenerationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY not configured.");
    }

    public async Task<string> GenerateAnswerAsync(string question, List<string> chunks)
    {
        var context = string.Join("\n\n---\n\n", chunks);

        var prompt = $"""
            You are a helpful assistant answering questions about a document.
            Use ONLY the context below to answer the question. 
            If the answer isn't in the context, say "I couldn't find that information in the document."

            Context:
            {context}

            Question: {question}

            Answer:
            """;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent";

        var requestBody = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini generateContent failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Gemini generateContent failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var answer = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        _logger.LogInformation("Generated answer: {Answer}", answer);

        return answer ?? string.Empty;
    }
}