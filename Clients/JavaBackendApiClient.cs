using System.Net.Http.Json;
using MegaFintradeRiskMonitor.Dtos.Project1;

namespace MegaFintradeRiskMonitor.Clients;

public class JavaBackendApiClient : IJavaBackendApiClient
{
    private const string HttpClientName = "JavaBackendApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JavaBackendApiClient> _logger;

    public JavaBackendApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<JavaBackendApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> IsBackendReachableAsync(CancellationToken cancellationToken = default)
    {
        var endpointsToCheck = new[]
        {
            "/api/reports/summary",
            "/api/import/audit",
            "/api/import/rejections",
            "/api/positions"
        };

        foreach (var endpoint in endpointsToCheck)
        {
            var reachable = await IsEndpointReachableAsync(endpoint, cancellationToken);

            if (reachable)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<JavaBackendReportSummaryDto?> GetReportSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            return await client.GetFromJsonAsync<JavaBackendReportSummaryDto>(
                "/api/reports/summary",
                cancellationToken);
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "Java backend report summary request timed out or was cancelled.");
            return null;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to call Java backend report summary endpoint.");
            return null;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "Java backend report summary response has an unsupported content type.");
            return null;
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Failed to parse Java backend report summary response.");
            return null;
        }
    }

    public async Task<IReadOnlyList<JavaBackendImportAuditDto>> GetImportAuditAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            var audits = await client.GetFromJsonAsync<List<JavaBackendImportAuditDto>>(
                "/api/import/audit",
                cancellationToken);

            return audits ?? new List<JavaBackendImportAuditDto>();
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "Java backend import audit request timed out or was cancelled.");
            return new List<JavaBackendImportAuditDto>();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to call Java backend import audit endpoint.");
            return new List<JavaBackendImportAuditDto>();
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "Java backend import audit response has an unsupported content type.");
            return new List<JavaBackendImportAuditDto>();
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Failed to parse Java backend import audit response.");
            return new List<JavaBackendImportAuditDto>();
        }
    }

    public async Task<IReadOnlyList<JavaBackendImportRejectionDto>> GetImportRejectionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            var rejections = await client.GetFromJsonAsync<List<JavaBackendImportRejectionDto>>(
                "/api/import/rejections",
                cancellationToken);

            return rejections ?? new List<JavaBackendImportRejectionDto>();
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "Java backend import rejections request timed out or was cancelled.");
            return new List<JavaBackendImportRejectionDto>();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Failed to call Java backend import rejections endpoint.");
            return new List<JavaBackendImportRejectionDto>();
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "Java backend import rejections response has an unsupported content type.");
            return new List<JavaBackendImportRejectionDto>();
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Failed to parse Java backend import rejections response.");
            return new List<JavaBackendImportRejectionDto>();
        }
    }

    private async Task<bool> IsEndpointReachableAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await client.GetAsync(endpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogInformation(
                "Java backend endpoint {Endpoint} returned HTTP status {StatusCode}.",
                endpoint,
                response.StatusCode);

            return false;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "Java backend reachability check timed out or was cancelled for endpoint {Endpoint}.",
                endpoint);

            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Java backend is not reachable at endpoint {Endpoint}.",
                endpoint);

            return false;
        }
    }
}