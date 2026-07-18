using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Services.EmbeddingServices;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly string _apiKey;
    private const string ModelName = "gemini-embedding-001";

    public EmbeddingService(HttpClient httpClient, ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY not configured.");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:embedContent?key={_apiKey}";

        var requestBody = JsonSerializer.Serialize(new
        {
            model = $"models/{ModelName}",
            content = new
            {
                parts = new[] { new { text } }
            }
        });

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini embedding request failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Gemini embedding request failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var valuesArray = doc.RootElement.GetProperty("embedding").GetProperty("values");

        var embedding = new float[valuesArray.GetArrayLength()];
        for (var i = 0; i < embedding.Length; i++)
        {
            embedding[i] = valuesArray[i].GetSingle();
        }

        _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

        return embedding;
    }
}