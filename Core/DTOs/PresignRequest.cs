namespace Core.DTOs;

public sealed record PresignRequest(string FileName, string? UserId = null);
