namespace Services.TextChunkingServices;

public interface ITextChunkingService
{
    List<string> ChunkText(string text);
}