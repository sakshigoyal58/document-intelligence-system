using System.Text.Json.Serialization;

namespace Core.DTOs;

public class UpdateStatusRequest
{
    [JsonPropertyName("documentId")]
    public required string DocumentId { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("s3Key")]
    public string? S3Key { get; set; }
}
