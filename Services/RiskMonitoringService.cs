using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Services;

public class RiskMonitoringService : IRiskMonitoringService
{
    private readonly IJavaBackendApiClient _javaBackendApiClient;
    private readonly IAlertRuleEngine _alertRuleEngine;
    private readonly IAlertService _alertService;
    private readonly RiskMonitorDbContext _dbContext;
    private readonly IOptionsMonitor<JavaBackendApiOptions> _javaBackendApiOptions;
    private readonly ILogger<RiskMonitoringService> _logger;

    public RiskMonitoringService(
        IJavaBackendApiClient javaBackendApiClient,
        IAlertRuleEngine alertRuleEngine,
        IAlertService alertService,
        RiskMonitorDbContext dbContext,
        IOptionsMonitor<JavaBackendApiOptions> javaBackendApiOptions,
        ILogger<RiskMonitoringService> logger)
    {
        _javaBackendApiClient = javaBackendApiClient;
        _alertRuleEngine = alertRuleEngine;
        _alertService = alertService;
        _dbContext = dbContext;
        _javaBackendApiOptions = javaBackendApiOptions;
        _logger = logger;
    }

    public async Task<RiskMonitoringRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAtUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Risk monitoring run started at {StartedAtUtc}.",
            startedAtUtc);

        var javaBackendReachable = await _javaBackendApiClient
            .IsBackendReachableAsync(cancellationToken);

        JavaBackendReportSummaryDto? reportSummary = null;
        IReadOnlyList<JavaBackendImportAuditDto> importAudits = Array.Empty<JavaBackendImportAuditDto>();
        IReadOnlyList<JavaBackendImportRejectionDto> importRejections = Array.Empty<JavaBackendImportRejectionDto>();

        var reportSummaryAvailable = false;
        var importAuditAvailable = false;
        var importRejectionsAvailable = false;
        var portfolioMonitoringAvailable = false;
        var symbolMonitoringAvailable = false;
        var validSymbols = new List<string>();

        if (javaBackendReachable)
        {
            reportSummary = await _javaBackendApiClient
                .GetReportSummaryAsync(cancellationToken);

            importAudits = await _javaBackendApiClient
                .GetImportAuditAsync(cancellationToken);

            importRejections = await _javaBackendApiClient
                .GetImportRejectionsAsync(cancellationToken);

            reportSummaryAvailable = reportSummary is not null;
            importAuditAvailable = importAudits.Count > 0;
            importRejectionsAvailable = importRejections.Count > 0;
            portfolioMonitoringAvailable = reportSummary is not null;
            validSymbols = GetValidSymbols(reportSummary);
            symbolMonitoringAvailable = validSymbols.Count > 0;

            _logger.LogInformation(
                "Risk monitoring run polled Java backend. ReportSummaryAvailable={ReportSummaryAvailable}, ImportAuditCount={ImportAuditCount}, ImportRejectionCount={ImportRejectionCount}, SymbolCount={SymbolCount}, SymbolMonitoringAvailable={SymbolMonitoringAvailable}.",
                reportSummaryAvailable,
                importAudits.Count,
                importRejections.Count,
                validSymbols.Count,
                symbolMonitoringAvailable);
        }
        else
        {
            _logger.LogWarning(
                "Risk monitoring run could not reach Mega Fintrade Backend Java.");
        }

        var evaluationRequest = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = javaBackendReachable,
            ReportSummary = reportSummary,
            ImportAudits = importAudits,
            ImportRejections = importRejections,
            EvaluationTimeUtc = DateTime.UtcNow
        };

        var evaluationResult = _alertRuleEngine.Evaluate(evaluationRequest);

        _logger.LogInformation(
            "Risk monitoring rule evaluation completed. AlertCandidateCount={AlertCandidateCount}, SystemAlertCandidateCount={SystemAlertCandidateCount}, SymbolAlertCandidateCount={SymbolAlertCandidateCount}, PortfolioRulesEvaluated={PortfolioRulesEvaluated}, SymbolRulesEvaluated={SymbolRulesEvaluated}, SymbolMetricCount={SymbolMetricCount}.",
            evaluationResult.AlertCandidateCount,
            evaluationResult.SystemAlertCandidateCount,
            evaluationResult.SymbolAlertCandidateCount,
            evaluationResult.PortfolioRulesEvaluated,
            evaluationResult.SymbolRulesEvaluated,
            evaluationResult.SymbolMetricCount);

        var resolvedAlerts = await _alertService.ResolveStaleAlertsAsync(
            evaluationResult.AlertCandidates,
            cancellationToken);

        _logger.LogInformation(
            "Risk monitoring stale alert reconciliation completed. ResolvedAlertCount={ResolvedAlertCount}.",
            resolvedAlerts.Count);

        var savedAlerts = await _alertService.SaveAlertCandidatesAsync(
            evaluationResult.AlertCandidates,
            cancellationToken);

        _logger.LogInformation(
            "Risk monitoring alert save completed. CandidateCount={CandidateCount}, SavedAlertCount={SavedAlertCount}.",
            evaluationResult.AlertCandidateCount,
            savedAlerts.Count);

        var activeAlertCount = await _dbContext.RiskAlerts
            .CountAsync(alert => alert.IsActive, cancellationToken);

        var criticalAlertCount = await _dbContext.RiskAlerts
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Critical,
                cancellationToken);

        var highAlertCount = await _dbContext.RiskAlerts
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.High,
                cancellationToken);

        var mediumAlertCount = await _dbContext.RiskAlerts
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Medium,
                cancellationToken);

        var lowAlertCount = await _dbContext.RiskAlerts
            .CountAsync(
                alert => alert.IsActive && alert.Severity == AlertSeverity.Low,
                cancellationToken);

        var status = javaBackendReachable
            ? "COMPLETED"
            : "JAVA_BACKEND_UNAVAILABLE";

        var message = BuildMonitoringMessage(
            javaBackendReachable,
            reportSummaryAvailable,
            portfolioMonitoringAvailable,
            symbolMonitoringAvailable,
            validSymbols.Count,
            evaluationResult.AlertCandidateCount,
            savedAlerts.Count,
            resolvedAlerts.Count);

        var snapshot = new MonitoringSnapshot
        {
            JavaBackendReachable = javaBackendReachable,
            JavaBackendBaseUrl = _javaBackendApiOptions.CurrentValue.BaseUrl,
            ReportSummaryAvailable = reportSummaryAvailable,
            ImportAuditAvailable = importAuditAvailable,
            ImportRejectionsAvailable = importRejectionsAvailable,
            PortfolioMonitoringAvailable = portfolioMonitoringAvailable,
            SymbolMonitoringAvailable = symbolMonitoringAvailable,
            SymbolCount = validSymbols.Count,
            ActiveAlertCount = activeAlertCount,
            CriticalAlertCount = criticalAlertCount,
            HighAlertCount = highAlertCount,
            MediumAlertCount = mediumAlertCount,
            LowAlertCount = lowAlertCount,
            Status = status,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MonitoringSnapshots.Add(snapshot);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var completedAtUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Risk monitoring snapshot saved. SnapshotId={SnapshotId}, Status={Status}, JavaBackendReachable={JavaBackendReachable}, PortfolioMonitoringAvailable={PortfolioMonitoringAvailable}, SymbolMonitoringAvailable={SymbolMonitoringAvailable}, SymbolCount={SymbolCount}, ActiveAlertCount={ActiveAlertCount}.",
            snapshot.Id,
            snapshot.Status,
            snapshot.JavaBackendReachable,
            snapshot.PortfolioMonitoringAvailable,
            snapshot.SymbolMonitoringAvailable,
            snapshot.SymbolCount,
            snapshot.ActiveAlertCount);

        _logger.LogInformation(
            "Risk monitoring run completed at {CompletedAtUtc}. JavaBackendReachable={JavaBackendReachable}.",
            completedAtUtc,
            javaBackendReachable);

        return new RiskMonitoringRunResult
        {
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            JavaBackendReachable = javaBackendReachable,
            ReportSummaryAvailable = reportSummaryAvailable,
            ImportAuditAvailable = importAuditAvailable,
            ImportRejectionsAvailable = importRejectionsAvailable,
            PortfolioMonitoringAvailable = portfolioMonitoringAvailable,
            SymbolMonitoringAvailable = symbolMonitoringAvailable,
            SymbolCount = validSymbols.Count,
            Symbols = validSymbols,
            AlertCandidateCount = evaluationResult.AlertCandidateCount,
            SavedAlertCount = savedAlerts.Count,
            MonitoringSnapshotId = snapshot.Id,
            Status = status,
            Message = message
        };
    }

    private static List<string> GetValidSymbols(JavaBackendReportSummaryDto? reportSummary)
    {
        if (reportSummary?.Symbols is null || reportSummary.Symbols.Count == 0)
        {
            return new List<string>();
        }

        return reportSummary.Symbols
            .Where(symbolMetric => !string.IsNullOrWhiteSpace(symbolMetric.Symbol))
            .Select(symbolMetric => symbolMetric.Symbol.Trim().ToUpperInvariant())
            .Distinct()
            .OrderBy(symbol => symbol)
            .ToList();
    }

    private static string BuildMonitoringMessage(
        bool javaBackendReachable,
        bool reportSummaryAvailable,
        bool portfolioMonitoringAvailable,
        bool symbolMonitoringAvailable,
        int symbolCount,
        int alertCandidateCount,
        int savedAlertCount,
        int resolvedAlertCount)
    {
        if (!javaBackendReachable)
        {
            return $"Monitoring run completed, but Java backend was unavailable. Alert candidates generated={alertCandidateCount}, new alerts saved={savedAlertCount}, stale alerts resolved={resolvedAlertCount}.";
        }

        if (!reportSummaryAvailable)
        {
            return $"Monitoring run completed, but Java backend report summary was unavailable. Alert candidates generated={alertCandidateCount}, new alerts saved={savedAlertCount}, stale alerts resolved={resolvedAlertCount}.";
        }

        if (portfolioMonitoringAvailable && !symbolMonitoringAvailable)
        {
            return $"Monitoring run completed with portfolio-level monitoring only. No symbol-level metrics were returned by the Java backend. Alert candidates generated={alertCandidateCount}, new alerts saved={savedAlertCount}, stale alerts resolved={resolvedAlertCount}.";
        }

        return $"Monitoring run completed with portfolio-level and symbol-level monitoring. Symbol count={symbolCount}. Alert candidates generated={alertCandidateCount}, new alerts saved={savedAlertCount}, stale alerts resolved={resolvedAlertCount}.";
    }
}