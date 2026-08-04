
using Core.Models;

namespace Business.Validation;

public class PdfValidator : IPdfValidator
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public ValidationResult Validate(string fileName, long fileSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Fail("Not a PDF file");

        if (fileSize <= 0)
            return Fail("Empty file");

        if (fileSize > MaxFileSize)
            return Fail($"File exceeds {MaxFileSize / (1024 * 1024)}MB limit");

        return Success();
    }

    private static ValidationResult Success() => new() { IsValid = true };

    private static ValidationResult Fail(string error) => new() { IsValid = false, Error = error };
}

