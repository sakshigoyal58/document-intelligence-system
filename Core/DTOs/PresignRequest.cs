using System.Text.Json.Serialization;

namespace Core.DTOs;


public class PresignRequest
{
    [JsonPropertyName("fileName")]
    public required string FileName { get; set; }

    [JsonPropertyName("userId")]
    public required string UserId { get; set; }
}
