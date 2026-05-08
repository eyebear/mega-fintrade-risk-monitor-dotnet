using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class AlertRuleEngineTests
{
    [Fact]
    public void Evaluate_ReturnsCriticalAlert_WhenJavaBackendUnavailable()
    {
        var engine = CreateEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = false,
            ReportSummary = null,
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.Equal(1, result.AlertCandidateCount);

        var alert = result.AlertCandidates.Single();

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.JavaBackendUnavailable, alert.Type);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal("JavaBackendApi", alert.SourceEndpoint);
        Assert.Equal("reachable=false", alert.SourceValue);
        Assert.Equal("reachable=true", alert.ThresholdValue);
    }

    [Fact]
    public void Evaluate_ReturnsImportFailureAlert_WhenLatestImportAuditFailed()
    {
        var engine = CreateEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = CreateHealthyPortfolioSummary(),
            ImportAudits = new List<JavaBackendImportAuditDto>
            {
                new()
                {
                    Id = 1,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    Status = "COMPLETED",
                    StartedAtUtc = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAtUtc = new DateTime(2026, 5, 7, 10, 1, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = 2,
                    ImportType = "BACKTEST_RESULTS",
                    SourceFile = "backtest_results.csv",
                    Status = "FAILED",
                    StartedAtUtc = new DateTime(2026, 5, 7, 11, 0, 0, DateTimeKind.Utc),
                    CompletedAtUtc = new DateTime(2026, 5, 7, 11, 1, 0, DateTimeKind.Utc)
                }
            },
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.ImportFailure, alert.Type);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal("/api/import/audit", alert.SourceEndpoint);
        Assert.Equal("FAILED", alert.SourceValue);
    }

    [Fact]
    public void Evaluate_ReturnsCsvRejectionAlert_WhenRejectionCountGreaterThanThreshold()
    {
        var engine = CreateEngine(csvRejectionThreshold: 0);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = CreateHealthyPortfolioSummary(),
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = new List<JavaBackendImportRejectionDto>
            {
                new()
                {
                    Id = 5,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    RowNumber = 10,
                    Reason = "Invalid decimal value",
                    CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
                }
            },
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.CsvRejectionsFound, alert.Type);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);
        Assert.Equal("/api/import/rejections", alert.SourceEndpoint);
        Assert.Equal("1", alert.SourceValue);
        Assert.Equal("0", alert.ThresholdValue);
    }

    [Fact]
    public void Evaluate_ReturnsPortfolioDrawdownAlert_WhenDrawdownBreachesThreshold()
    {
        var engine = CreateEngine(maxDrawdownThreshold: -0.20m);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.25m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.DrawdownBreach, alert.Type);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal("/api/reports/summary", alert.SourceEndpoint);
        Assert.Equal("-0.25", alert.SourceValue);
        Assert.Equal("-0.20", alert.ThresholdValue);
    }

    [Fact]
    public void Evaluate_DoesNotReturnPortfolioDrawdownAlert_WhenDrawdownDoesNotBreachThreshold()
    {
        var engine = CreateEngine(maxDrawdownThreshold: -0.20m);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.Empty(result.AlertCandidates);
    }

    [Fact]
    public void Evaluate_ReturnsPortfolioLowSharpeAlert_WhenSharpeBelowThreshold()
    {
        var engine = CreateEngine(minimumSharpeRatio: 1.0m);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 0.80m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.LowSharpeRatio, alert.Type);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);
        Assert.Equal("0.80", alert.SourceValue);
        Assert.Equal("1.0", alert.ThresholdValue);
    }

    [Fact]
    public void Evaluate_ReturnsPortfolioStaleDataAlert_WhenLatestEquityDateTooOld()
    {
        var engine = CreateEngine(staleDataDays: 3);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 1),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.StaleEquityData, alert.Type);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);
        Assert.Equal("2026-05-01", alert.SourceValue);
    }

    [Fact]
    public void Evaluate_ReturnsEmptyReportDataAlert_WhenAllReportCountsAreZero()
    {
        var engine = CreateEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = null,
                PortfolioMaxDrawdown = null,
                LatestEquityDate = null,
                RiskMetricRowCount = 0,
                BacktestResultRowCount = 0,
                StrategySignalRowCount = 0,
                EquityCurveRowCount = 0,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        var alert = Assert.Single(result.AlertCandidates);

        Assert.Null(alert.Symbol);
        Assert.Equal(AlertType.EmptyReportData, alert.Type);
        Assert.Equal(AlertSeverity.Low, alert.Severity);
        Assert.Equal("/api/reports/summary", alert.SourceEndpoint);
    }

    [Fact]
    public void Evaluate_ReturnsSymbolSpecificAlerts_WhenSymbolMetricsBreachThresholds()
    {
        var engine = CreateEngine(
            maxDrawdownThreshold: -0.20m,
            minimumSharpeRatio: 1.0m,
            staleDataDays: 3);

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>
                {
                    new()
                    {
                        Symbol = "AAPL",
                        SharpeRatio = 0.70m,
                        MaxDrawdown = -0.25m,
                        LatestDataDate = new DateOnly(2026, 5, 1)
                    }
                }
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.Equal(3, result.AlertCandidateCount);
        Assert.Equal(0, result.SystemAlertCandidateCount);
        Assert.Equal(3, result.SymbolAlertCandidateCount);
        Assert.True(result.SymbolRulesEvaluated);
        Assert.Equal(1, result.SymbolMetricCount);

        Assert.All(result.AlertCandidates, alert => Assert.Equal("AAPL", alert.Symbol));

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Type == AlertType.StaleEquityData &&
            alert.Severity == AlertSeverity.Medium);
    }

    [Fact]
    public void Evaluate_IgnoresBlankSymbolNames()
    {
        var engine = CreateEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>
                {
                    new()
                    {
                        Symbol = " ",
                        SharpeRatio = 0.50m,
                        MaxDrawdown = -0.50m,
                        LatestDataDate = new DateOnly(2026, 5, 1)
                    }
                }
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.Empty(result.AlertCandidates);
        Assert.False(result.SymbolRulesEvaluated);
        Assert.Equal(0, result.SymbolMetricCount);
    }

    private static AlertRuleEngine CreateEngine(
        decimal maxDrawdownThreshold = -0.20m,
        decimal minimumSharpeRatio = 1.0m,
        int staleDataDays = 3,
        int csvRejectionThreshold = 0)
    {
        var options = new AlertRuleOptions
        {
            MaxDrawdownThreshold = maxDrawdownThreshold,
            MinimumSharpeRatio = minimumSharpeRatio,
            StaleDataDays = staleDataDays,
            CsvRejectionThreshold = csvRejectionThreshold
        };

        return new AlertRuleEngine(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static JavaBackendReportSummaryDto CreateHealthyPortfolioSummary()
    {
        return new JavaBackendReportSummaryDto
        {
            PortfolioSharpeRatio = 1.25m,
            PortfolioMaxDrawdown = -0.10m,
            LatestEquityDate = new DateOnly(2026, 5, 7),
            RiskMetricRowCount = 4,
            BacktestResultRowCount = 12,
            StrategySignalRowCount = 16,
            EquityCurveRowCount = 100,
            Symbols = new List<JavaBackendSymbolRiskMetricDto>()
        };
    }
}