namespace Core.Configuration;

public sealed class AppSettings
{
    public const string SectionName = "App";

    public string? GeminiApiKey { get; init; }
    public string? ChunkQueueUrl { get; init; }
    public string? UploadBucket { get; init; }
    public string? TextractSnsTopicArn { get; init; }
    public string? TextractSnsRoleArn { get; init; }
}
