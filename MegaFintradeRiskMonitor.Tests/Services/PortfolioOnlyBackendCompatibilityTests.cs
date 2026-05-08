using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using AppDbContext = global::MegaFintradeRiskMonitor.Data.RiskMonitorDbContext;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class PortfolioOnlyBackendCompatibilityTests
{
    [Fact]
    public void AlertRuleEngine_HandlesPortfolioOnlyResponse_WithEmptySymbolList()
    {
        var engine = CreateAlertRuleEngine();

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

        Assert.True(result.PortfolioRulesEvaluated);
        Assert.False(result.SymbolRulesEvaluated);
        Assert.Equal(0, result.SymbolMetricCount);
        Assert.Empty(result.AlertCandidates);
    }

    [Fact]
    public void AlertRuleEngine_HandlesPortfolioOnlyResponse_WithNullSymbolList()
    {
        var engine = CreateAlertRuleEngine();

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
                Symbols = null
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.True(result.PortfolioRulesEvaluated);
        Assert.False(result.SymbolRulesEvaluated);
        Assert.Equal(0, result.SymbolMetricCount);
        Assert.Empty(result.AlertCandidates);
    }

    [Fact]
    public void AlertRuleEngine_GeneratesPortfolioAlerts_WithoutSymbolData()
    {
        var engine = CreateAlertRuleEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 0.75m,
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

        Assert.True(result.PortfolioRulesEvaluated);
        Assert.False(result.SymbolRulesEvaluated);
        Assert.Equal(0, result.SymbolMetricCount);
        Assert.Equal(2, result.AlertCandidateCount);
        Assert.Equal(2, result.SystemAlertCandidateCount);
        Assert.Equal(0, result.SymbolAlertCandidateCount);

        Assert.All(result.AlertCandidates, alert => Assert.Null(alert.Symbol));

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);
    }

    [Fact]
    public async Task RiskMonitoringService_HandlesPortfolioOnlyResponse_WithEmptySymbolList()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
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
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.Equal("COMPLETED", result.Status);
        Assert.True(result.JavaBackendReachable);
        Assert.True(result.ReportSummaryAvailable);
        Assert.True(result.PortfolioMonitoringAvailable);
        Assert.False(result.SymbolMonitoringAvailable);
        Assert.Equal(0, result.SymbolCount);
        Assert.Empty(result.Symbols);
        Assert.Equal(0, result.AlertCandidateCount);
        Assert.Equal(0, result.SavedAlertCount);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal("COMPLETED", snapshot.Status);
        Assert.True(snapshot.JavaBackendReachable);
        Assert.True(snapshot.ReportSummaryAvailable);
        Assert.True(snapshot.PortfolioMonitoringAvailable);
        Assert.False(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(0, snapshot.SymbolCount);
        Assert.Equal(0, snapshot.ActiveAlertCount);
    }

    [Fact]
    public async Task RiskMonitoringService_HandlesPortfolioOnlyResponse_WithNullSymbolList()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 1.25m,
                PortfolioMaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = null
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.Equal("COMPLETED", result.Status);
        Assert.True(result.PortfolioMonitoringAvailable);
        Assert.False(result.SymbolMonitoringAvailable);
        Assert.Equal(0, result.SymbolCount);
        Assert.Empty(result.Symbols);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.True(snapshot.PortfolioMonitoringAvailable);
        Assert.False(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(0, snapshot.SymbolCount);
    }

    [Fact]
    public async Task RiskMonitoringService_SavesPortfolioAlerts_WithoutSymbolData()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = new JavaBackendReportSummaryDto
            {
                PortfolioSharpeRatio = 0.75m,
                PortfolioMaxDrawdown = -0.25m,
                LatestEquityDate = new DateOnly(2026, 5, 7),
                RiskMetricRowCount = 4,
                BacktestResultRowCount = 12,
                StrategySignalRowCount = 16,
                EquityCurveRowCount = 100,
                Symbols = new List<JavaBackendSymbolRiskMetricDto>()
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.Equal("COMPLETED", result.Status);
        Assert.True(result.PortfolioMonitoringAvailable);
        Assert.False(result.SymbolMonitoringAvailable);
        Assert.Equal(0, result.SymbolCount);
        Assert.Equal(2, result.AlertCandidateCount);
        Assert.Equal(2, result.SavedAlertCount);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, alert => Assert.Null(alert.Symbol));

        Assert.Contains(alerts, alert =>
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(alerts, alert =>
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal(2, snapshot.ActiveAlertCount);
        Assert.Equal(1, snapshot.HighAlertCount);
        Assert.Equal(1, snapshot.MediumAlertCount);
        Assert.False(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(0, snapshot.SymbolCount);
    }

    private static AlertRuleEngine CreateAlertRuleEngine()
    {
        return new AlertRuleEngine(
            Microsoft.Extensions.Options.Options.Create(
                new AlertRuleOptions
                {
                    MaxDrawdownThreshold = -0.20m,
                    MinimumSharpeRatio = 1.0m,
                    StaleDataDays = 3,
                    CsvRejectionThreshold = 0
                }));
    }

    private static RiskMonitoringService CreateRiskMonitoringService(
        FakeJavaBackendApiClient javaBackendClient,
        AppDbContext dbContext)
    {
        var alertRuleEngine = CreateAlertRuleEngine();

        var alertService = new AlertService(
            dbContext,
            NullLogger<AlertService>.Instance);

        var javaBackendOptions = new TestOptionsMonitor<JavaBackendApiOptions>(
            new JavaBackendApiOptions
            {
                BaseUrl = "http://localhost:8080",
                TimeoutSeconds = 10
            });

        return new RiskMonitoringService(
            javaBackendClient,
            alertRuleEngine,
            alertService,
            dbContext,
            javaBackendOptions,
            NullLogger<RiskMonitoringService>.Instance);
    }
}