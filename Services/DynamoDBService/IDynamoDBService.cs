namespace Services.DynamoDb;

public interface IDynamoDbFileService
{
    Task AddFileRecordAsync(string fileId, string fileName, long fileSize);
    Task UpdateFileStatusAsync(string fileId, string status, string? errorMessage = null);
}