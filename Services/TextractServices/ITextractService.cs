namespace Services.TextractServices;

public interface ITextractService
{
    Task<string> StartTextDetectionJobAsync(string s3Key);
}