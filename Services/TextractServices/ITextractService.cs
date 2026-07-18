namespace Services.TextractServices;

public interface ITextractService
{
    Task<string> StartTextDetectionJobAsync(string s3Key);
    Task<string> GetExtractedTextAsync(string jobId);
}