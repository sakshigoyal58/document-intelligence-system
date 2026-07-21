namespace Services.QueueServices;

public interface IChunkQueueService
{
   Task SendChunksForIndexingAsync(string jobId, string documentId, List<ChunkPayload> chunks);
}

public class ChunkPayload
{
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
}