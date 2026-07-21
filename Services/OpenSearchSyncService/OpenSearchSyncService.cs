using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.DTOs;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.OpenSearch;

public class OpenSearchSyncService : IOpenSearchSyncService
{
    private readonly HttpClient _httpClient;
    private readonly OpenSearchSetting _settings;
    private readonly ILogger<OpenSearchSyncService> _logger;

    public OpenSearchSyncService(HttpClient httpClient, IOptions<OpenSearchSetting> options, ILogger<OpenSearchSyncService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _logger.LogInformation("OpenSearch Endpoint: {Endpoint}", _settings.Endpoint);
        _logger.LogInformation("OpenSearch IndexName: {IndexName}", _settings.IndexName);

        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
        {
            throw new Exception("OpenSearch Endpoint is missing in configuration");
        }

        if (string.IsNullOrWhiteSpace(_settings.IndexName))
        {
            throw new Exception("OpenSearch IndexName is missing in configuration");
        }

        if (!string.IsNullOrWhiteSpace(_settings.Endpoint))
        {
            _httpClient.BaseAddress = new Uri(_settings.Endpoint.TrimEnd('/'));
        }
    }

    public async Task IndexDocumentAsync(OpenSearchDocumentPayload payload)
    {
        var indexUrl = $"{_settings.IndexName}/_doc/{payload.DocumentId}";
        var json = JsonSerializer.Serialize(payload);

        _logger.LogInformation(
            "Indexing document {DocumentId} to OpenSearch path {IndexUrl}",
            payload.DocumentId,
            indexUrl);

        var response = await _httpClient.PutAsync(
    indexUrl,
    new StringContent(json, Encoding.UTF8, "application/json")
);

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "OpenSearch response for {DocumentId}: {StatusCode} - Body: {Body}",
            payload.DocumentId,
            response.StatusCode,
            responseBody);

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DocumentSearchResponse>> SearchDocumentsByNameAsync(string searchText)
    {
        var searchUrl = $"{_settings.IndexName}/_search";

        var query = new
        {
            query = new
            {
                @bool = new
                {
                    should = new object[]
                    {
                    new
                    {
                        fuzzy = new
                        {
                            FileName = new
                            {
                                value = searchText,
                                fuzziness = "AUTO"
                            }
                        }
                    },
                    new
                    {
                        wildcard = new Dictionary<string, object>
                        {
                            ["FileName.keyword"] = new
                            {
                                value = $"*{searchText.ToLower()}*",
                                case_insensitive = true
                            }
                        }
                    }
                    },
                    minimum_should_match = 1
                }
            },
            size = 10
        };

        var json = JsonSerializer.Serialize(query);

        _logger.LogInformation(
            "Searching OpenSearch for text: {SearchText} with query: {Query}",
            searchText,
            json);

        var response = await _httpClient.PostAsync(
            searchUrl,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "OpenSearch search response: {StatusCode} - Body: {Body}",
            response.StatusCode,
            responseBody);

        response.EnsureSuccessStatusCode();

        var results = ParseSearchResults(responseBody);

        _logger.LogInformation(
            "Found {Count} matching documents for search text: {SearchText}",
            results.Count,
            searchText);

        return results;
    }

    private List<DocumentSearchResponse> ParseSearchResults(string responseBody)
    {
        var results = new List<DocumentSearchResponse>();

        using (JsonDocument doc = JsonDocument.Parse(responseBody))
        {
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("hits", out JsonElement hits) &&
                hits.TryGetProperty("hits", out JsonElement hitsArray))
            {
                foreach (JsonElement hit in hitsArray.EnumerateArray())
                {
                    if (hit.TryGetProperty("_source", out JsonElement source))
                    {
                        if (source.TryGetProperty("DocumentId", out JsonElement docId) &&
                            source.TryGetProperty("FileName", out JsonElement fileName))
                        {
                            var documentId = docId.GetString();
                            var documentName = fileName.GetString();

                            if (!string.IsNullOrEmpty(documentId) && !string.IsNullOrEmpty(documentName))
                            {
                                results.Add(new DocumentSearchResponse(documentId, documentName));
                            }
                        }
                    }
                }
            }
        }

        return results;
    }

    public async Task<string> CreateChunksIndexAsync()
    {
        var indexMapping = new
        {
            settings = new
            {
                index = new { knn = true }
            },
            mappings = new
            {
                properties = new
                {
                    documentId = new { type = "keyword" },
                    chunkIndex = new { type = "integer" },
                    chunkText = new { type = "text" },
                    chunkVector = new
                    {
                        type = "knn_vector",
                        dimension = 3072,
                        method = new
                        {
                            name = "hnsw",
                            space_type = "cosinesimil",
                            engine = "lucene"
                        }
                    },
                    createdAt = new { type = "date" }
                }
            }
        };

        var json = JsonSerializer.Serialize(indexMapping);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("document-chunks", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "Create index response: {StatusCode} - {Body}",
            response.StatusCode, responseBody);

        return responseBody;
    }

    public async Task IndexChunkAsync(string documentId, int chunkIndex, string chunkText, float[] vector)
    {
        var chunkId = $"{documentId}_{chunkIndex}"; // stable ID — prevents duplicates on reprocessing
        var indexUrl = $"document-chunks/_doc/{chunkId}";

        var payload = new
        {
            documentId,
            chunkIndex,
            chunkText,
            chunkVector = vector,
            createdAt = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(payload);

        _logger.LogInformation(
            "Indexing chunk {ChunkIndex} for document {DocumentId} to {IndexUrl}",
            chunkIndex, documentId, indexUrl);

        var response = await _httpClient.PutAsync(
            indexUrl,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "OpenSearch chunk index response: {StatusCode} - {Body}",
            response.StatusCode, responseBody);

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> SearchChunksByVectorAsync(string documentId, float[] vector, int topK = 3)
    {
        var searchUrl = "document-chunks/_search";

        var query = new
        {
            size = topK,
            query = new
            {
                @bool = new
                {
                    must = new object[]
                    {
                    new
                    {
                        knn = new
                        {
                            chunkVector = new
                            {
                                vector,
                                k = topK
                            }
                        }
                    }
                    },
                    filter = new object[]
                    {
                    new { term = new { documentId } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(query);

        _logger.LogInformation(
            "Searching document-chunks for documentId {DocumentId}, top {TopK}",
            documentId, topK);

        var response = await _httpClient.PostAsync(
            searchUrl,
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation(
            "OpenSearch chunk search response: {StatusCode} - {Body}",
            response.StatusCode, responseBody);

        response.EnsureSuccessStatusCode();

        var chunks = new List<string>();

        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("hits", out var hits) &&
            hits.TryGetProperty("hits", out var hitsArray))
        {
            foreach (var hit in hitsArray.EnumerateArray())
            {
                if (hit.TryGetProperty("_source", out var source) &&
                    source.TryGetProperty("chunkText", out var chunkTextElement))
                {
                    var chunkText = chunkTextElement.GetString();
                    if (!string.IsNullOrEmpty(chunkText))
                    {
                        chunks.Add(chunkText);
                    }
                }
            }
        }

        _logger.LogInformation("Found {Count} matching chunk(s) for document {DocumentId}", chunks.Count, documentId);

        return chunks;
    }
}