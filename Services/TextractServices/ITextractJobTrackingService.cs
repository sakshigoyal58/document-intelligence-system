namespace Services.TextractServices;

public interface ITextractJobTrackingService
{
    Task SaveJobAsync(string jobId, string documentId);

    Task<string?> GetDocumentIdAsync(string jobId);

    Task UpdateStatusAsync(string jobId, string status);
    Task<string?> GetStatusAsync(string jobId);
}