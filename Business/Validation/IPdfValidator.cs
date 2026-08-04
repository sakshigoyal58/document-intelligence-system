using Core.Models;

namespace Business.Validation;
public interface IPdfValidator
{
    ValidationResult Validate(string fileName, long fileSize);
}   