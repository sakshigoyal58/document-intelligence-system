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
                fuzzy = new
                {
                    FileName = new
                    {
                        value = searchText,
                        fuzziness = "AUTO"
                    }
                }
            },
            size = 10 // Return all matching documents
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

        // Parse response and map to DocumentSearchResponse
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
}