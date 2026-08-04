using Amazon.SQS;
using Amazon.SQS.Model;
using Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Services.QueueServices;

public class ChunkQueueService : IChunkQueueService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<ChunkQueueService> _logger;
    private readonly string _queueUrl;

    public ChunkQueueService(IAmazonSQS sqsClient, ILogger<ChunkQueueService> logger, IOptions<AppSettings> options)
    {
        _sqsClient = sqsClient;
        _logger = logger;

        _queueUrl = options.Value.ChunkQueueUrl
            ?? throw new InvalidOperationException("CHUNK_QUEUE_URL not configured.");
    }

    public async Task SendChunksForIndexingAsync(string jobId, string documentId, List<ChunkPayload> chunks)
    {

        var messageBody = JsonSerializer.Serialize(
   new { jobId, documentId, chunks },
   new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = messageBody
        };

        var response = await _sqsClient.SendMessageAsync(request);

        _logger.LogInformation(
            "Sent chunk batch for document {DocumentId} to SQS. MessageId: {MessageId}",
            documentId, response.MessageId);
    }
}