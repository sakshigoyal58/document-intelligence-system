
using Core.Models;

namespace Services.OpenSearch;

public interface IOpenSearchSyncService
{
    Task IndexDocumentAsync(OpenSearchDocumentPayload payload);
}