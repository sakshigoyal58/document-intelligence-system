using Core.Models;

namespace Services.DynamoDb;

public interface IDynamoDbService
{
    Task AddFileRecordAsync(string fileId, string fileName, long fileSize);
    Task UpdateFileStatusAsync(string fileId, string status, string? errorMessage = null);
    Task<List<DocumentEntity>> GetDocumentsAsync(DocumentQuery query);
}