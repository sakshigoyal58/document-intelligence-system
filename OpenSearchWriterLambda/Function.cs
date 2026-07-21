using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.DependencyInjection;
using Services.OpenSearch;
using Services.TextractServices;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OpenSearchWriterLambda;

public class Function
{
    private static readonly IServiceProvider _provider =
        DependencyInjection.BuildServiceProvider();

    private readonly ILogger<Function> _logger;
    private readonly IOpenSearchSyncService _openSearchSyncService;
    private readonly ITextractJobTrackingService _textractJobTrackingService;

    public Function()
    {
        _logger = _provider.GetRequiredService<ILogger<Function>>();
        _openSearchSyncService = _provider.GetRequiredService<IOpenSearchSyncService>();
        _textractJobTrackingService = _provider.GetRequiredService<ITextractJobTrackingService>();
    }

    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        _logger.LogInformation("OpenSearchWriterLambda received {Count} SQS message(s)", sqsEvent.Records.Count);

        foreach (var record in sqsEvent.Records)
        {
            try
            {
                using var doc = JsonDocument.Parse(record.Body);
                var root = doc.RootElement;

                var jobId = root.GetProperty("jobId").GetString();
                var documentId = root.GetProperty("documentId").GetString();
                var chunksArray = root.GetProperty("chunks");

                

                _logger.LogInformation(
                    "Processing {Count} chunk(s) for document {DocumentId}, job {JobId}",
                    chunksArray.GetArrayLength(), documentId, jobId);

                foreach (var chunkElement in chunksArray.EnumerateArray())
                {
                    var chunkIndex = chunkElement.GetProperty("chunkIndex").GetInt32();
                    var chunkText = chunkElement.GetProperty("chunkText").GetString();
                    var vectorArray = chunkElement.GetProperty("vector");

                    var vector = new float[vectorArray.GetArrayLength()];
                    for (var i = 0; i < vector.Length; i++)
                    {
                        vector[i] = vectorArray[i].GetSingle();
                    }

                    await _openSearchSyncService.IndexChunkAsync(documentId!, chunkIndex, chunkText!, vector);

                    _logger.LogInformation(
                        "Indexed chunk {ChunkIndex} for document {DocumentId}",
                        chunkIndex, documentId);
                }

                await _textractJobTrackingService.UpdateStatusAsync(jobId!, "COMPLETED");

                _logger.LogInformation(
                    "Successfully indexed all chunks and marked job {JobId} COMPLETED for document {DocumentId}",
                    jobId, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to process SQS message. Error: {ErrorMessage}. Details: {ErrorDetails}",
                    ex.Message, ex.ToString());
                throw; // rethrow so SQS retries / eventually sends to DLQ
            }
        }
    }
}