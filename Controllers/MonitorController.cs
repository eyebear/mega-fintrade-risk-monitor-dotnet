using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Dtos.Monitor;
using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
[Route("api/monitor")]
public class MonitorController : ControllerBase
{
    private readonly JavaBackendApiOptions _javaBackendApiOptions;
    private readonly AiIntegrationOptions _aiIntegrationOptions;
    private readonly AlertRuleOptions _alertRuleOptions;
    private readonly MonitoringOptions _monitoringOptions;
    private readonly IJavaBackendApiClient _javaBackendApiClient;
    private readonly IAlertRuleEngine _alertRuleEngine;
    private readonly IRiskMonitoringService _riskMonitoringService;
    private readonly RiskMonitorDbContext _dbContext;

    public MonitorController(
        IOptions<JavaBackendApiOptions> javaBackendApiOptions,
        IOptions<AiIntegrationOptions> aiIntegrationOptions,
        IOptions<AlertRuleOptions> alertRuleOptions,
        IOptions<MonitoringOptions> monitoringOptions,
        IJavaBackendApiClient javaBackendApiClient,
        IAlertRuleEngine alertRuleEngine,
        IRiskMonitoringService riskMonitoringService,
        RiskMonitorDbContext dbContext)
    {
        _javaBackendApiOptions = javaBackendApiOptions.Value;
        _aiIntegrationOptions = aiIntegrationOptions.Value;
        _alertRuleOptions = alertRuleOptions.Value;
        _monitoringOptions = monitoringOptions.Value;
        _javaBackendApiClient = javaBackendApiClient;
        _alertRuleEngine = alertRuleEngine;
        _riskMonitoringService = riskMonitoringService;
        _dbContext = dbContext;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunMonitor(CancellationToken cancellationToken)
    {
        var result = await _riskMonitoringService.RunOnceAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var javaBackendReachable = await _javaBackendApiClient
            .IsBackendReachableAsync(cancellationToken);

        var latestSnapshot = await _dbContext.MonitoringSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var activeAlertCount = await _dbContext.RiskAlerts
            .AsNoTracking()
            .CountAsync(alert => alert.IsActive, cancellationToken);

        var criticalAlertCount = await _dbContext.RiskAlerts
            .AsNoTracking()
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Critical,
                cancellationToken);

        var highAlertCount = await _dbContext.RiskAlerts
            .AsNoTracking()
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.High,
                cancellationToken);

        var mediumAlertCount = await _dbContext.RiskAlerts
            .AsNoTracking()
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Medium,
                cancellationToken);

        var lowAlertCount = await _dbContext.RiskAlerts
            .AsNoTracking()
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Low,
                cancellationToken);

        return Ok(new
        {
            service = "mega-fintrade-risk-monitor-dotnet",
            status = javaBackendReachable ? "MONITOR_READY" : "JAVA_BACKEND_UNAVAILABLE",
            javaBackend = new
            {
                baseUrl = _javaBackendApiOptions.BaseUrl,
                timeoutSeconds = _javaBackendApiOptions.TimeoutSeconds,
                reachable = javaBackendReachable,
                plannedEndpoints = new[]
                {
                    "/api/reports/summary",
                    "/api/import/audit",
                    "/api/import/rejections"
                }
            },
            latestMonitoringSnapshot = latestSnapshot is null
                ? null
                : new
                {
                    id = latestSnapshot.Id,
                    status = latestSnapshot.Status,
                    message = latestSnapshot.Message,
                    createdAtUtc = latestSnapshot.CreatedAtUtc,
                    javaBackendReachable = latestSnapshot.JavaBackendReachable,
                    javaBackendBaseUrl = latestSnapshot.JavaBackendBaseUrl,
                    reportSummaryAvailable = latestSnapshot.ReportSummaryAvailable,
                    importAuditAvailable = latestSnapshot.ImportAuditAvailable,
                    importRejectionsAvailable = latestSnapshot.ImportRejectionsAvailable,
                    portfolioMonitoringAvailable = latestSnapshot.PortfolioMonitoringAvailable,
                    symbolMonitoringAvailable = latestSnapshot.SymbolMonitoringAvailable,
                    symbolCount = latestSnapshot.SymbolCount,
                    activeAlertCount = latestSnapshot.ActiveAlertCount,
                    criticalAlertCount = latestSnapshot.CriticalAlertCount,
                    highAlertCount = latestSnapshot.HighAlertCount,
                    mediumAlertCount = latestSnapshot.MediumAlertCount,
                    lowAlertCount = latestSnapshot.LowAlertCount
                },
            currentAlertSummary = new
            {
                activeAlertCount,
                criticalAlertCount,
                highAlertCount,
                mediumAlertCount,
                lowAlertCount
            },
            alertRules = new
            {
                maxDrawdownThreshold = _alertRuleOptions.MaxDrawdownThreshold,
                minimumSharpeRatio = _alertRuleOptions.MinimumSharpeRatio,
                staleDataDays = _alertRuleOptions.StaleDataDays,
                csvRejectionThreshold = _alertRuleOptions.CsvRejectionThreshold
            },
            monitoring = new
            {
                pollingIntervalSeconds = _monitoringOptions.PollingIntervalSeconds,
                manualRunEndpoint = "POST /api/monitor/run",
                statusEndpoint = "GET /api/monitor/status"
            },
            aiIntegration = new
            {
                enabled = _aiIntegrationOptions.Enabled,
                project5BaseUrl = _aiIntegrationOptions.Project5BaseUrl,
                mode = _aiIntegrationOptions.Enabled ? "OPTIONAL_EXTERNAL_SERVICE" : "DISABLED"
            },
            dynamicSymbolSupport = new
            {
                enabled = true,
                hardCodedSymbols = false,
                rule = "The monitor does not hard-code stock symbols. Symbol-level metrics are consumed dynamically when returned by the Java backend."
            },
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("java-backend/reachable")]
    public async Task<IActionResult> CheckJavaBackendReachability(CancellationToken cancellationToken)
    {
        var isReachable = await _javaBackendApiClient.IsBackendReachableAsync(cancellationToken);

        return Ok(new
        {
            javaBackendReachable = isReachable,
            baseUrl = _javaBackendApiOptions.BaseUrl,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("java-backend/report-summary")]
    public async Task<IActionResult> GetJavaBackendReportSummary(CancellationToken cancellationToken)
    {
        var reportSummary = await _javaBackendApiClient.GetReportSummaryAsync(cancellationToken);

        if (reportSummary is null)
        {
            return Ok(new
            {
                available = false,
                message = "Java backend report summary is unavailable or could not be parsed.",
                endpoint = "/api/reports/summary",
                portfolioMonitoringAvailable = false,
                symbolMonitoringAvailable = false,
                symbolCount = 0,
                symbols = Array.Empty<SymbolRiskStatusDto>(),
                timestampUtc = DateTime.UtcNow
            });
        }

        var symbolRiskStatuses = reportSummary.Symbols
            .Where(symbolMetric => !string.IsNullOrWhiteSpace(symbolMetric.Symbol))
            .Select(symbolMetric => new SymbolRiskStatusDto
            {
                Symbol = symbolMetric.Symbol.Trim(),
                SharpeRatio = symbolMetric.SharpeRatio,
                MaxDrawdown = symbolMetric.MaxDrawdown,
                LatestDataDate = symbolMetric.LatestDataDate
            })
            .OrderBy(symbolMetric => symbolMetric.Symbol)
            .ToList();

        return Ok(new
        {
            available = true,
            endpoint = "/api/reports/summary",
            portfolioMonitoringAvailable = true,
            symbolMonitoringAvailable = symbolRiskStatuses.Count > 0,
            symbolCount = symbolRiskStatuses.Count,
            dynamicSymbolSupport = new
            {
                enabled = true,
                hardCodedSymbols = false,
                message = symbolRiskStatuses.Count > 0
                    ? "Symbol-level metrics were returned by the Java backend and normalized dynamically."
                    : "No symbol-level metrics were returned by the Java backend. Portfolio-level monitoring remains active."
            },
            portfolio = new
            {
                sharpeRatio = reportSummary.PortfolioSharpeRatio,
                maxDrawdown = reportSummary.PortfolioMaxDrawdown,
                latestEquityDate = reportSummary.LatestEquityDate,
                riskMetricRowCount = reportSummary.RiskMetricRowCount,
                backtestResultRowCount = reportSummary.BacktestResultRowCount,
                strategySignalRowCount = reportSummary.StrategySignalRowCount,
                equityCurveRowCount = reportSummary.EquityCurveRowCount
            },
            symbols = symbolRiskStatuses,
            rawReportSummary = reportSummary,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("java-backend/import-audit")]
    public async Task<IActionResult> GetJavaBackendImportAudit(CancellationToken cancellationToken)
    {
        var audits = await _javaBackendApiClient.GetImportAuditAsync(cancellationToken);

        return Ok(new
        {
            available = audits.Count > 0,
            endpoint = "/api/import/audit",
            auditCount = audits.Count,
            latestAudit = audits.FirstOrDefault(),
            audits,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("java-backend/import-rejections")]
    public async Task<IActionResult> GetJavaBackendImportRejections(CancellationToken cancellationToken)
    {
        var rejections = await _javaBackendApiClient.GetImportRejectionsAsync(cancellationToken);

        return Ok(new
        {
            available = rejections.Count > 0,
            endpoint = "/api/import/rejections",
            rejectionCount = rejections.Count,
            latestRejection = rejections.FirstOrDefault(),
            rejections,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("rule-evaluation")]
    public async Task<IActionResult> EvaluateRules(CancellationToken cancellationToken)
    {
        var javaBackendReachable = await _javaBackendApiClient.IsBackendReachableAsync(cancellationToken);

        var reportSummary = javaBackendReachable
            ? await _javaBackendApiClient.GetReportSummaryAsync(cancellationToken)
            : null;

        var importAudits = javaBackendReachable
            ? await _javaBackendApiClient.GetImportAuditAsync(cancellationToken)
            : Array.Empty<JavaBackendImportAuditDto>();

        var importRejections = javaBackendReachable
            ? await _javaBackendApiClient.GetImportRejectionsAsync(cancellationToken)
            : Array.Empty<JavaBackendImportRejectionDto>();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = javaBackendReachable,
            ReportSummary = reportSummary,
            ImportAudits = importAudits,
            ImportRejections = importRejections,
            EvaluationTimeUtc = DateTime.UtcNow
        };

        var evaluationResult = _alertRuleEngine.Evaluate(request);

        return Ok(evaluationResult);
    }
}