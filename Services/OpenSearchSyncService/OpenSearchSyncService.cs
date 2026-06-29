using System.Net.Http;
using System.Text;
using System.Text.Json;
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

        _logger.LogInformation(
            "OpenSearch response for {DocumentId}: {StatusCode}",
            payload.DocumentId,
            response.StatusCode);

        response.EnsureSuccessStatusCode();
    }
}