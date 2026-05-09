using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Dtos.Ai;
using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Pages;

public class DashboardModel : PageModel
{
    private readonly IJavaBackendApiClient _javaBackendApiClient;
    private readonly IAlertService _alertService;
    private readonly IRiskMonitoringService _riskMonitoringService;
    private readonly IAiDecisionSupportClient _aiDecisionSupportClient;
    private readonly JavaBackendApiOptions _javaBackendApiOptions;
    private readonly AiIntegrationOptions _aiIntegrationOptions;

    public DashboardModel(
        IJavaBackendApiClient javaBackendApiClient,
        IAlertService alertService,
        IRiskMonitoringService riskMonitoringService,
        IAiDecisionSupportClient aiDecisionSupportClient,
        IOptions<JavaBackendApiOptions> javaBackendApiOptions,
        IOptions<AiIntegrationOptions> aiIntegrationOptions)
    {
        _javaBackendApiClient = javaBackendApiClient;
        _alertService = alertService;
        _riskMonitoringService = riskMonitoringService;
        _aiDecisionSupportClient = aiDecisionSupportClient;
        _javaBackendApiOptions = javaBackendApiOptions.Value;
        _aiIntegrationOptions = aiIntegrationOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public string? SymbolFilter { get; set; }

    public string JavaBackendBaseUrl { get; private set; } = string.Empty;

    public int JavaBackendTimeoutSeconds { get; private set; }

    public bool JavaBackendReachable { get; private set; }

    public DateTime CheckedAtUtc { get; private set; }

    public IReadOnlyList<RiskAlert> ActiveAlerts { get; private set; } =
        Array.Empty<RiskAlert>();

    public IReadOnlyList<RiskAlert> AlertHistory { get; private set; } =
        Array.Empty<RiskAlert>();

    public int ActiveAlertCount => ActiveAlerts.Count;

    public JavaBackendReportSummaryDto? PortfolioReportSummary { get; private set; }

    public bool PortfolioReportAvailable => PortfolioReportSummary is not null;

    public IReadOnlyList<JavaBackendSymbolRiskMetricDto> SymbolMetrics { get; private set; } =
        Array.Empty<JavaBackendSymbolRiskMetricDto>();

    public IReadOnlyList<JavaBackendImportAuditDto> ImportAudits { get; private set; } =
        Array.Empty<JavaBackendImportAuditDto>();

    public JavaBackendImportAuditDto? LatestImportAudit { get; private set; }

    public IReadOnlyList<JavaBackendImportRejectionDto> ImportRejections { get; private set; } =
        Array.Empty<JavaBackendImportRejectionDto>();

    public IReadOnlyList<JavaBackendImportRejectionDto> CurrentImportRejections { get; private set; } =
        Array.Empty<JavaBackendImportRejectionDto>();

    public JavaBackendImportRejectionDto? LatestImportRejection { get; private set; }

    public int CurrentRejectedRowCount => CurrentImportRejections.Count;

    public bool LatestImportSucceeded =>
        LatestImportAudit is not null &&
        string.Equals(LatestImportAudit.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

    public bool LatestImportFailed =>
        LatestImportAudit is not null &&
        string.Equals(LatestImportAudit.Status, "FAILED", StringComparison.OrdinalIgnoreCase);

    public string CurrentCsvRejectionHealthMessage
    {
        get
        {
            if (LatestImportAudit is null)
            {
                return "No import audit record is available yet.";
            }

            if (LatestImportSucceeded)
            {
                return "No current CSV rejection issue. Historical rejection rows are not counted against the latest successful import.";
            }

            if (LatestImportFailed && CurrentRejectedRowCount == 0)
            {
                return "Latest import failed, but no rejection row details were returned by the Java backend.";
            }

            if (CurrentRejectedRowCount == 0)
            {
                return "No current CSV rejection issue.";
            }

            return "Current CSV rejection issue detected for the latest failed import.";
        }
    }

    public RiskMonitoringRunResult? ManualRunResult { get; private set; }

    public AiIntegrationStatusDto? AiIntegrationStatus { get; private set; }

    public bool AiIntegrationEnabled => _aiIntegrationOptions.Enabled;

    public string AiAdvisorBaseUrl => _aiIntegrationOptions.Project5BaseUrl;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDashboardDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRunMonitorAsync(CancellationToken cancellationToken)
    {
        ManualRunResult = await _riskMonitoringService.RunOnceAsync(cancellationToken);

        await LoadDashboardDataAsync(cancellationToken);

        return Page();
    }

    private async Task LoadDashboardDataAsync(CancellationToken cancellationToken)
    {
        JavaBackendBaseUrl = _javaBackendApiOptions.BaseUrl;
        JavaBackendTimeoutSeconds = _javaBackendApiOptions.TimeoutSeconds;
        CheckedAtUtc = DateTime.UtcNow;

        AiIntegrationStatus = await _aiDecisionSupportClient
            .GetStatusAsync(cancellationToken);

        JavaBackendReachable = await _javaBackendApiClient
            .IsBackendReachableAsync(cancellationToken);

        ActiveAlerts = await _alertService
            .GetActiveAlertsAsync(SymbolFilter, cancellationToken);

        AlertHistory = await _alertService
            .GetAlertHistoryAsync(SymbolFilter, cancellationToken);

        if (!JavaBackendReachable)
        {
            PortfolioReportSummary = null;
            SymbolMetrics = Array.Empty<JavaBackendSymbolRiskMetricDto>();
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>();
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>();
            CurrentImportRejections = Array.Empty<JavaBackendImportRejectionDto>();
            LatestImportAudit = null;
            LatestImportRejection = null;
            return;
        }

        PortfolioReportSummary = await _javaBackendApiClient
            .GetReportSummaryAsync(cancellationToken);

        SymbolMetrics = GetFilteredSymbolMetrics(
            PortfolioReportSummary,
            SymbolFilter);

        ImportAudits = await _javaBackendApiClient
            .GetImportAuditAsync(cancellationToken);

        LatestImportAudit = ImportAudits
            .OrderByDescending(audit => audit.CompletedAtUtc ?? audit.StartedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(audit => audit.Id ?? 0)
            .FirstOrDefault();

        ImportRejections = await _javaBackendApiClient
            .GetImportRejectionsAsync(cancellationToken);

        CurrentImportRejections = GetCurrentImportRejections(
            LatestImportAudit,
            ImportRejections);

        LatestImportRejection = CurrentImportRejections
            .OrderByDescending(rejection => rejection.CreatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(rejection => rejection.Id ?? 0)
            .FirstOrDefault();
    }

    private static IReadOnlyList<JavaBackendImportRejectionDto> GetCurrentImportRejections(
        JavaBackendImportAuditDto? latestAudit,
        IReadOnlyList<JavaBackendImportRejectionDto> importRejections)
    {
        if (latestAudit is null)
        {
            return Array.Empty<JavaBackendImportRejectionDto>();
        }

        if (string.Equals(latestAudit.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<JavaBackendImportRejectionDto>();
        }

        if (!string.Equals(latestAudit.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<JavaBackendImportRejectionDto>();
        }

        if (!latestAudit.StartedAtUtc.HasValue)
        {
            return importRejections
                .OrderByDescending(rejection => rejection.CreatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(rejection => rejection.Id ?? 0)
                .ToList();
        }

        return importRejections
            .Where(rejection =>
                rejection.CreatedAtUtc.HasValue &&
                rejection.CreatedAtUtc.Value >= latestAudit.StartedAtUtc.Value)
            .OrderByDescending(rejection => rejection.CreatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(rejection => rejection.Id ?? 0)
            .ToList();
    }

    private static IReadOnlyList<JavaBackendSymbolRiskMetricDto> GetFilteredSymbolMetrics(
        JavaBackendReportSummaryDto? reportSummary,
        string? symbolFilter)
    {
        if (reportSummary?.Symbols is null || reportSummary.Symbols.Count == 0)
        {
            return Array.Empty<JavaBackendSymbolRiskMetricDto>();
        }

        var query = reportSummary.Symbols
            .Where(symbolMetric => !string.IsNullOrWhiteSpace(symbolMetric.Symbol))
            .Select(symbolMetric => new JavaBackendSymbolRiskMetricDto
            {
                Symbol = symbolMetric.Symbol.Trim().ToUpperInvariant(),
                SharpeRatio = symbolMetric.SharpeRatio,
                MaxDrawdown = symbolMetric.MaxDrawdown,
                LatestDataDate = symbolMetric.LatestDataDate
            });

        if (!string.IsNullOrWhiteSpace(symbolFilter))
        {
            var normalizedFilter = symbolFilter.Trim().ToUpperInvariant();

            query = query.Where(symbolMetric => symbolMetric.Symbol == normalizedFilter);
        }

        return query
            .OrderBy(symbolMetric => symbolMetric.Symbol)
            .ToList();
    }

    public string GetSymbolAlertStatus(string symbol)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        var symbolAlerts = ActiveAlerts
            .Where(alert => string.Equals(
                alert.Symbol,
                normalizedSymbol,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (symbolAlerts.Count == 0)
        {
            return "Normal";
        }

        if (symbolAlerts.Any(alert => alert.Type == AlertType.DrawdownBreach))
        {
            return "Drawdown Breach";
        }

        if (symbolAlerts.Any(alert => alert.Type == AlertType.LowSharpeRatio))
        {
            return "Low Sharpe";
        }

        if (symbolAlerts.Any(alert => alert.Type == AlertType.StaleEquityData))
        {
            return "Stale Data";
        }

        return "Alert";
    }

    public static string GetAlertScopeLabel(RiskAlert alert)
    {
        if (string.IsNullOrWhiteSpace(alert.Symbol))
        {
            return "Portfolio/System";
        }

        return alert.Symbol;
    }

    public static string GetSeverityCssClass(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "severity-critical",
            AlertSeverity.High => "severity-high",
            AlertSeverity.Medium => "severity-medium",
            AlertSeverity.Low => "severity-low",
            _ => "severity-low"
        };
    }

    public static string GetImportStatusCssClass(string? status)
    {
        if (string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return "status-bad";
        }

        if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return "status-good";
        }

        return "status-warning";
    }

    public static string GetCsvRejectionStatusCssClass(int currentRejectedRowCount)
    {
        return currentRejectedRowCount > 0
            ? "status-warning"
            : "status-good";
    }

    public static string GetAiStatusCssClass(AiIntegrationStatusDto? status)
    {
        if (status is null)
        {
            return "status-warning";
        }

        if (!status.Enabled)
        {
            return "status-neutral";
        }

        return status.AdvisorReachable ? "status-good" : "status-warning";
    }

    public static string FormatDecimal(decimal? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        return value.Value.ToString("0.####");
    }

    public static string FormatDate(DateOnly? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        return value.Value.ToString("yyyy-MM-dd");
    }

    public static string FormatDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        return value.Value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string FormatCount(int? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        return value.Value.ToString();
    }

    public static string FormatNullableText(string? value, string fallback = "N/A")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim();
    }

    public static string FormatImportType(string? importType)
    {
        return FormatNullableText(importType, "Aggregate import");
    }

    public static string FormatSourceFile(string? sourceFile)
    {
        return FormatNullableText(sourceFile, "Multiple pipeline files");
    }

    public static string NormalizeSymbolForDisplay(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return string.Empty;
        }

        return symbol.Trim().ToUpperInvariant();
    }
}