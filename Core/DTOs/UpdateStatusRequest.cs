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
}
