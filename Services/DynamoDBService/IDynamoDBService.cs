using Core.Models;
using Core.DTOs;
namespace Services.DynamoDb;

public interface IDynamoDbService
{
    Task AddFileRecordAsync(string fileId, string fileName, long fileSize);
    Task UpdateFileStatusAsync(UpdateStatusRequest updateStatusRequest);
    Task<List<DocumentEntity>> GetDocumentsAsync(DocumentQuery query);
}