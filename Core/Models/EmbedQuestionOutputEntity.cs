namespace Core.Models;
public class EmbedQuestionOutputEntity
{
    public string DocumentId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
}
