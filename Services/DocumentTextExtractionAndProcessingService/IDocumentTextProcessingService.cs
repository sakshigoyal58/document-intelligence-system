namespace Services.DocumentTextExtractionAndProcessingService;

public interface IDocumentTextProcessingService
{
   Task ProcessTextFromDocumentAsync(string documentId, string s3Key);
}