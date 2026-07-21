using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.TextractServices;
using Services.TextChunkingServices;
using Services.EmbeddingServices;
using Services.QueueServices;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TextractResultLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly ITextractService _textractService;
    private readonly ITextractJobTrackingService _textractJobTrackingService;
    private readonly ITextChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChunkQueueService _chunkQueueService;

    public Function()
    {
        _logger = _provider.GetRequiredService<ILogger<Function>>();
        _textractService = _provider.GetRequiredService<ITextractService>();
        _textractJobTrackingService = _provider.GetRequiredService<ITextractJobTrackingService>();
        _chunkingService = _provider.GetRequiredService<ITextChunkingService>();
        _embeddingService = _provider.GetRequiredService<IEmbeddingService>();
        _chunkQueueService = _provider.GetRequiredService<IChunkQueueService>();
    }

    public async Task FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
    {
        _logger.LogInformation("TextractResultLambda received {Count} SNS records", snsEvent.Records.Count);

        foreach (var record in snsEvent.Records)
        {
            var messageBody = record.Sns.Message;

            _logger.LogInformation("Raw SNS message: {Message}", messageBody);

            try
            {
                using var doc = JsonDocument.Parse(messageBody);
                var root = doc.RootElement;

                var jobId = root.GetProperty("JobId").GetString();
                var status = root.GetProperty("Status").GetString();

                _logger.LogInformation(
                    "Parsed Textract notification. JobId: {JobId}, Status: {Status}",
                    jobId, status);

                if (string.IsNullOrWhiteSpace(jobId))
                {
                    _logger.LogWarning("SNS message missing JobId — skipping");
                    continue;
                }

                var documentId = await _textractJobTrackingService.GetDocumentIdAsync(jobId);

                if (documentId == null)
                {
                    _logger.LogWarning("Could not find documentId for JobId {JobId} — skipping", jobId);
                    continue;
                }

                _logger.LogInformation("Resolved JobId {JobId} to DocumentId {DocumentId}", jobId, documentId);

                var currentStatus = await _textractJobTrackingService.GetStatusAsync(jobId);
                if (currentStatus == "COMPLETED")
                {
                    _logger.LogInformation("Job {JobId} already COMPLETED — skipping duplicate processing", jobId);
                    continue;
                }

                if (status == "SUCCEEDED")
                {
                    var extractedText = await _textractService.GetExtractedTextAsync(jobId);

                    _logger.LogInformation(
                        "Extracted text for DocumentId {DocumentId}: {Preview}",
                        documentId,
                        extractedText.Length > 200 ? extractedText[..200] + "..." : extractedText);

                    var chunks = _chunkingService.ChunkText(extractedText);

                    _logger.LogInformation(
                        "Chunked document {DocumentId} into {ChunkCount} chunk(s)",
                        documentId, chunks.Count);

                    var chunkPayloads = new List<ChunkPayload>();

                    foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
                    {
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk);

                        _logger.LogInformation(
                            "Generated embedding for chunk {Index} of document {DocumentId}. Vector length: {Length}",
                            index, documentId, embedding.Length);

                        chunkPayloads.Add(new ChunkPayload
                        {
                            ChunkIndex = index,
                            ChunkText = chunk,
                            Vector = embedding
                        });
                    }

                    await _chunkQueueService.SendChunksForIndexingAsync(jobId, documentId, chunkPayloads);

                    _logger.LogInformation(
                        "Sent {Count} chunk(s) to SQS for document {DocumentId}. OpenSearchWriterLambda will handle indexing and mark status COMPLETED.",
                        chunkPayloads.Count, documentId);

                    // NOTE: status is NOT marked COMPLETED here anymore.
                    // OpenSearchWriterLambda marks it COMPLETED once chunks are
                    // actually saved to OpenSearch.
                }
                else
                {
                    await _textractJobTrackingService.UpdateStatusAsync(jobId, "FAILED");
                    _logger.LogWarning("Textract job {JobId} for document {DocumentId} FAILED", jobId, documentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to process SNS message. Error: {ErrorMessage}. Details: {ErrorDetails}",
                    ex.Message, ex.ToString());
            }
        }
    }
}