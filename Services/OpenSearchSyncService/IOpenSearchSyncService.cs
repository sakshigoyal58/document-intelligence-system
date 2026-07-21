
using Core.DTOs;
using Core.Models;

namespace Services.OpenSearch;

public interface IOpenSearchSyncService
{
    Task<string> CreateChunksIndexAsync();
    Task IndexDocumentAsync(OpenSearchDocumentPayload payload);
    Task<List<DocumentSearchResponse>> SearchDocumentsByNameAsync(string searchText);

    Task IndexChunkAsync(string documentId, int chunkIndex, string chunkText, float[] vector);
}