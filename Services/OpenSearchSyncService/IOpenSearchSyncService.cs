
using Core.DTOs;
using Core.Models;

namespace Services.OpenSearch;

public interface IOpenSearchSyncService
{
    Task IndexDocumentAsync(OpenSearchDocumentPayload payload);
    Task<List<DocumentSearchResponse>> SearchDocumentsByNameAsync(string searchText);
}