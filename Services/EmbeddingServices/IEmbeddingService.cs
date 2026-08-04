namespace Services.EmbeddingServices;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}