
using Core.Models;

namespace Business.Validation;

public class PdfValidator
{
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB

    public ValidationResult Validate(string fileName, long fileSize)
    {
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Fail("Not a PDF file");

        if (fileSize == 0)
            return Fail("Empty file");

        if (fileSize > MAX_FILE_SIZE)
            return Fail($"File exceeds {MAX_FILE_SIZE / (1024 * 1024)}MB limit");

        return Success();
    }

    private static ValidationResult Success()
    {
        return new() { IsValid = true };
    }

    private static ValidationResult Fail(string error) => new() { IsValid = false, Error = error };
}

