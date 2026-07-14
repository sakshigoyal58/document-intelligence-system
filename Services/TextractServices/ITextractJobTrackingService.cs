namespace Services.TextractServices;

public interface ITextractJobTrackingService
{
    Task SaveJobAsync(string jobId, string documentId);
}