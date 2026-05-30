namespace Core.DTOs;

public sealed record FileKeyResponse(string FileKey, string PresignedUrl, string Message);
