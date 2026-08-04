using Microsoft.Extensions.Logging;
using Services.S3Service;

namespace Services.DocumentTextExtractionAndProcessingService;

public class DocumentTextProcessingService : IDocumentTextProcessingService
{
    private readonly IS3Service _s3Service;
    private readonly ILogger<DocumentTextProcessingService> _logger;

    public DocumentTextProcessingService(
        IS3Service s3Service,
        ILogger<DocumentTextProcessingService> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    public async Task ProcessTextFromDocumentAsync(string documentId, string s3Key)
    {
        try
        {
            _logger.LogInformation("Starting text processing for document: {S3Key}", s3Key);

            using var documentStream = await _s3Service.DownloadFileFromS3Async(s3Key);
            // Here you would implement the logic to process the text from the document stream.
            // For example, you might use a library to extract text from PDFs or other document formats.

            _logger.LogInformation("Completed text processing for document: {S3Key}", s3Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing text from document: {S3Key}", s3Key);
            throw;
        }
    }
}