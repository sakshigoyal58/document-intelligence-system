namespace Core.Models;

public class GeminiQABuildInputEntity
{
    public string Question { get; set; } = string.Empty;
    public List<string> Chunks { get; set; } = new();
}