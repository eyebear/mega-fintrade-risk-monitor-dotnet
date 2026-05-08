using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Services;

public class AlertRuleEngine : IAlertRuleEngine
{
    private readonly AlertRuleOptions _alertRuleOptions;

    public AlertRuleEngine(IOptions<AlertRuleOptions> alertRuleOptions)
    {
        _alertRuleOptions = alertRuleOptions.Value;
    }

    public AlertRuleEvaluationResult Evaluate(AlertRuleEvaluationRequest request)
    {
        var alerts = new List<RiskAlertCandidate>();

        var portfolioRulesEvaluated = request.JavaBackendReachable &&
                                      request.ReportSummary is not null;

        var symbolMetrics = GetValidSymbolMetrics(request.ReportSummary);
        var symbolRulesEvaluated = symbolMetrics.Count > 0;

        EvaluateSystemRules(request, alerts);
        EvaluatePortfolioRules(request, alerts);
        EvaluateSymbolRules(request, symbolMetrics, alerts);

        return new AlertRuleEvaluationResult
        {
            EvaluatedAtUtc = request.EvaluationTimeUtc,
            JavaBackendReachable = request.JavaBackendReachable,
            PortfolioRulesEvaluated = portfolioRulesEvaluated,
            SymbolRulesEvaluated = symbolRulesEvaluated,
            SymbolMetricCount = symbolMetrics.Count,
            AlertCandidates = alerts
        };
    }

    private void EvaluateSystemRules(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        EvaluateJavaBackendUnavailableRule(request, alerts);
        EvaluateImportFailureRule(request, alerts);
        EvaluateCsvRejectionRule(request, alerts);
    }

    private static void EvaluateJavaBackendUnavailableRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (request.JavaBackendReachable)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.JavaBackendUnavailable,
            Severity = AlertSeverity.Critical,
            Message = "Mega Fintrade Backend Java is unavailable. The risk monitor cannot retrieve current report, import audit, or rejection data.",
            SourceEndpoint = "JavaBackendApi",
            SourceValue = "reachable=false",
            ThresholdValue = "reachable=true"
        });
    }

    private static void EvaluateImportFailureRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        if (request.ImportAudits.Count == 0)
        {
            return;
        }

        var latestAudit = request.ImportAudits
            .OrderByDescending(audit => audit.CompletedAtUtc ?? audit.StartedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(audit => audit.Id ?? 0)
            .FirstOrDefault();

        if (latestAudit is null)
        {
            return;
        }

        if (!string.Equals(latestAudit.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var importType = string.IsNullOrWhiteSpace(latestAudit.ImportType)
            ? "UNKNOWN_IMPORT_TYPE"
            : latestAudit.ImportType.Trim();

        var sourceFile = string.IsNullOrWhiteSpace(latestAudit.SourceFile)
            ? "UNKNOWN_SOURCE_FILE"
            : latestAudit.SourceFile.Trim();

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.ImportFailure,
            Severity = AlertSeverity.High,
            Message = $"Latest Java backend import failed. ImportType={importType}, SourceFile={sourceFile}.",
            SourceEndpoint = "/api/import/audit",
            SourceValue = latestAudit.Status,
            ThresholdValue = "Status must not be FAILED"
        });
    }

    private void EvaluateCsvRejectionRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        var rejectionCount = request.ImportRejections.Count;
        var threshold = _alertRuleOptions.CsvRejectionThreshold;

        if (rejectionCount <= threshold)
        {
            return;
        }

        var latestRejection = request.ImportRejections
            .OrderByDescending(rejection => rejection.CreatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(rejection => rejection.Id ?? 0)
            .FirstOrDefault();

        var latestReason = latestRejection?.Reason;

        if (string.IsNullOrWhiteSpace(latestReason))
        {
            latestReason = "No rejection reason provided.";
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.CsvRejectionsFound,
            Severity = AlertSeverity.Medium,
            Message = $"Java backend reported {rejectionCount} rejected CSV row(s). Latest reason: {latestReason}",
            SourceEndpoint = "/api/import/rejections",
            SourceValue = rejectionCount.ToString(),
            ThresholdValue = threshold.ToString()
        });
    }

    private void EvaluatePortfolioRules(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        EvaluateEmptyReportDataRule(request, alerts);
        EvaluatePortfolioMaxDrawdownRule(request, alerts);
        EvaluatePortfolioSharpeRatioRule(request, alerts);
        EvaluatePortfolioStaleDataRule(request, alerts);
    }

    private void EvaluateEmptyReportDataRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        var reportSummary = request.ReportSummary;

        if (reportSummary is null)
        {
            alerts.Add(new RiskAlertCandidate
            {
                Symbol = null,
                Type = AlertType.EmptyReportData,
                Severity = AlertSeverity.Low,
                Message = "Java backend report summary is unavailable. The monitor could not read portfolio report data.",
                SourceEndpoint = "/api/reports/summary",
                SourceValue = "reportSummary=null",
                ThresholdValue = "reportSummary must be available"
            });

            return;
        }

        var riskMetricRowCount = reportSummary.RiskMetricRowCount ?? 0;
        var backtestResultRowCount = reportSummary.BacktestResultRowCount ?? 0;
        var strategySignalRowCount = reportSummary.StrategySignalRowCount ?? 0;
        var equityCurveRowCount = reportSummary.EquityCurveRowCount ?? 0;

        var allRowCountsAreZero =
            riskMetricRowCount == 0 &&
            backtestResultRowCount == 0 &&
            strategySignalRowCount == 0 &&
            equityCurveRowCount == 0;

        if (!allRowCountsAreZero)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.EmptyReportData,
            Severity = AlertSeverity.Low,
            Message = "Java backend report summary contains no imported report rows. Risk metrics, backtest results, strategy signals, and equity curve counts are all zero.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = "riskMetricRowCount=0;backtestResultRowCount=0;strategySignalRowCount=0;equityCurveRowCount=0",
            ThresholdValue = "At least one report row count should be greater than 0"
        });
    }

    private void EvaluatePortfolioMaxDrawdownRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        var reportSummary = request.ReportSummary;

        if (reportSummary is null)
        {
            return;
        }

        if (!reportSummary.PortfolioMaxDrawdown.HasValue)
        {
            return;
        }

        var portfolioMaxDrawdown = reportSummary.PortfolioMaxDrawdown.Value;
        var threshold = _alertRuleOptions.MaxDrawdownThreshold;

        if (portfolioMaxDrawdown > threshold)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.DrawdownBreach,
            Severity = AlertSeverity.High,
            Message = $"Portfolio max drawdown breached the configured threshold. Current max drawdown={portfolioMaxDrawdown}, threshold={threshold}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = portfolioMaxDrawdown.ToString(),
            ThresholdValue = threshold.ToString()
        });
    }

    private void EvaluatePortfolioSharpeRatioRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        var reportSummary = request.ReportSummary;

        if (reportSummary is null)
        {
            return;
        }

        if (!reportSummary.PortfolioSharpeRatio.HasValue)
        {
            return;
        }

        var portfolioSharpeRatio = reportSummary.PortfolioSharpeRatio.Value;
        var threshold = _alertRuleOptions.MinimumSharpeRatio;

        if (portfolioSharpeRatio >= threshold)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.LowSharpeRatio,
            Severity = AlertSeverity.Medium,
            Message = $"Portfolio Sharpe ratio is below the configured threshold. Current Sharpe ratio={portfolioSharpeRatio}, threshold={threshold}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = portfolioSharpeRatio.ToString(),
            ThresholdValue = threshold.ToString()
        });
    }

    private void EvaluatePortfolioStaleDataRule(
        AlertRuleEvaluationRequest request,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        var reportSummary = request.ReportSummary;

        if (reportSummary is null)
        {
            return;
        }

        if (!reportSummary.LatestEquityDate.HasValue)
        {
            return;
        }

        var latestEquityDate = reportSummary.LatestEquityDate.Value;
        var evaluationDate = DateOnly.FromDateTime(request.EvaluationTimeUtc);
        var staleDataDays = _alertRuleOptions.StaleDataDays;

        var daysOld = evaluationDate.DayNumber - latestEquityDate.DayNumber;

        if (daysOld <= staleDataDays)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = null,
            Type = AlertType.StaleEquityData,
            Severity = AlertSeverity.Medium,
            Message = $"Portfolio equity data is stale. Latest equity date={latestEquityDate}, evaluation date={evaluationDate}, age in days={daysOld}, allowed stale days={staleDataDays}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = latestEquityDate.ToString("yyyy-MM-dd"),
            ThresholdValue = $"Latest equity date must be within {staleDataDays} day(s)"
        });
    }

    private void EvaluateSymbolRules(
        AlertRuleEvaluationRequest request,
        IReadOnlyList<JavaBackendSymbolRiskMetricDto> symbolMetrics,
        List<RiskAlertCandidate> alerts)
    {
        if (!request.JavaBackendReachable)
        {
            return;
        }

        if (symbolMetrics.Count == 0)
        {
            return;
        }

        foreach (var symbolMetric in symbolMetrics)
        {
            EvaluateSingleSymbolRules(request, symbolMetric, alerts);
        }
    }

    private void EvaluateSingleSymbolRules(
        AlertRuleEvaluationRequest request,
        JavaBackendSymbolRiskMetricDto symbolMetric,
        List<RiskAlertCandidate> alerts)
    {
        EvaluateSymbolMaxDrawdownRule(symbolMetric, alerts);
        EvaluateSymbolSharpeRatioRule(symbolMetric, alerts);
        EvaluateSymbolStaleDataRule(request, symbolMetric, alerts);
    }

    private void EvaluateSymbolMaxDrawdownRule(
        JavaBackendSymbolRiskMetricDto symbolMetric,
        List<RiskAlertCandidate> alerts)
    {
        if (!symbolMetric.MaxDrawdown.HasValue)
        {
            return;
        }

        var symbol = symbolMetric.Symbol.Trim();
        var maxDrawdown = symbolMetric.MaxDrawdown.Value;
        var threshold = _alertRuleOptions.MaxDrawdownThreshold;

        if (maxDrawdown > threshold)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = symbol,
            Type = AlertType.DrawdownBreach,
            Severity = AlertSeverity.High,
            Message = $"Symbol {symbol} max drawdown breached the configured threshold. Current max drawdown={maxDrawdown}, threshold={threshold}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = maxDrawdown.ToString(),
            ThresholdValue = threshold.ToString()
        });
    }

    private void EvaluateSymbolSharpeRatioRule(
        JavaBackendSymbolRiskMetricDto symbolMetric,
        List<RiskAlertCandidate> alerts)
    {
        if (!symbolMetric.SharpeRatio.HasValue)
        {
            return;
        }

        var symbol = symbolMetric.Symbol.Trim();
        var sharpeRatio = symbolMetric.SharpeRatio.Value;
        var threshold = _alertRuleOptions.MinimumSharpeRatio;

        if (sharpeRatio >= threshold)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = symbol,
            Type = AlertType.LowSharpeRatio,
            Severity = AlertSeverity.Medium,
            Message = $"Symbol {symbol} Sharpe ratio is below the configured threshold. Current Sharpe ratio={sharpeRatio}, threshold={threshold}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = sharpeRatio.ToString(),
            ThresholdValue = threshold.ToString()
        });
    }

    private void EvaluateSymbolStaleDataRule(
        AlertRuleEvaluationRequest request,
        JavaBackendSymbolRiskMetricDto symbolMetric,
        List<RiskAlertCandidate> alerts)
    {
        if (!symbolMetric.LatestDataDate.HasValue)
        {
            return;
        }

        var symbol = symbolMetric.Symbol.Trim();
        var latestDataDate = symbolMetric.LatestDataDate.Value;
        var evaluationDate = DateOnly.FromDateTime(request.EvaluationTimeUtc);
        var staleDataDays = _alertRuleOptions.StaleDataDays;

        var daysOld = evaluationDate.DayNumber - latestDataDate.DayNumber;

        if (daysOld <= staleDataDays)
        {
            return;
        }

        alerts.Add(new RiskAlertCandidate
        {
            Symbol = symbol,
            Type = AlertType.StaleEquityData,
            Severity = AlertSeverity.Medium,
            Message = $"Symbol {symbol} data is stale. Latest data date={latestDataDate}, evaluation date={evaluationDate}, age in days={daysOld}, allowed stale days={staleDataDays}.",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = latestDataDate.ToString("yyyy-MM-dd"),
            ThresholdValue = $"Latest symbol data date must be within {staleDataDays} day(s)"
        });
    }

    private static IReadOnlyList<JavaBackendSymbolRiskMetricDto> GetValidSymbolMetrics(
        JavaBackendReportSummaryDto? reportSummary)
    {
        if (reportSummary?.Symbols is null || reportSummary.Symbols.Count == 0)
        {
            return Array.Empty<JavaBackendSymbolRiskMetricDto>();
        }

        return reportSummary.Symbols
            .Where(symbolMetric => !string.IsNullOrWhiteSpace(symbolMetric.Symbol))
            .Select(symbolMetric => new JavaBackendSymbolRiskMetricDto
            {
                Symbol = symbolMetric.Symbol.Trim(),
                SharpeRatio = symbolMetric.SharpeRatio,
                MaxDrawdown = symbolMetric.MaxDrawdown,
                LatestDataDate = symbolMetric.LatestDataDate
            })
            .OrderBy(symbolMetric => symbolMetric.Symbol)
            .ToList();
    }
}