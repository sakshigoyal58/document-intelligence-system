namespace Core.Helpers;

public static class DocumentStorageKey
{
    private const string Prefix = "sakshi/";

    public static string Build(string documentId, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var safeFileName = Uri.EscapeDataString(Path.GetFileName(fileName.Trim()));
        return $"{Prefix}{documentId}__{safeFileName}";
    }

    public static (string DocumentId, string FileName) Parse(string s3Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        var normalizedKey = s3Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            ? s3Key[Prefix.Length..]
            : s3Key;

        var separatorIndex = normalizedKey.IndexOf("__", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            throw new FormatException($"S3 key '{s3Key}' is not in the expected document format.");
        }

        var documentId = normalizedKey[..separatorIndex];
        var encodedFileName = normalizedKey[(separatorIndex + 2)..];
        var fileName = Uri.UnescapeDataString(encodedFileName);

        return (documentId, string.IsNullOrWhiteSpace(fileName) ? "unknown.pdf" : fileName);
    }
}
