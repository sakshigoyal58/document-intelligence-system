namespace Core.Models;

public class OpenSearchQuestionChunkOutput
{
    public string Question { get; set; } = string.Empty;
    public List<string> Chunks { get; set; } = new();
}
