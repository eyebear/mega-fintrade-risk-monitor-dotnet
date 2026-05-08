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

public class DynamicSymbolBackendCompatibilityTests
{
    [Fact]
    public void AlertRuleEngine_EvaluatesDynamicSymbolMetrics_WhenSymbolsAreReturned()
    {
        var engine = CreateAlertRuleEngine();

        var request = new AlertRuleEvaluationRequest
        {
            JavaBackendReachable = true,
            ReportSummary = CreateReportSummaryWithDynamicSymbols(),
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.True(result.PortfolioRulesEvaluated);
        Assert.True(result.SymbolRulesEvaluated);
        Assert.Equal(3, result.SymbolMetricCount);

        Assert.Equal(4, result.AlertCandidateCount);
        Assert.Equal(0, result.SystemAlertCandidateCount);
        Assert.Equal(4, result.SymbolAlertCandidateCount);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Symbol == "MSFT" &&
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Symbol == "MSFT" &&
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Symbol == "NVDA" &&
            alert.Type == AlertType.StaleEquityData &&
            alert.Severity == AlertSeverity.Medium);

        Assert.Contains(result.AlertCandidates, alert =>
            alert.Symbol == "TSLA" &&
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);
    }

    [Fact]
    public void AlertRuleEngine_IgnoresBlankSymbols_WhenSymbolListContainsInvalidRows()
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
                Symbols = new List<JavaBackendSymbolRiskMetricDto>
                {
                    new()
                    {
                        Symbol = " ",
                        SharpeRatio = 0.50m,
                        MaxDrawdown = -0.50m,
                        LatestDataDate = new DateOnly(2026, 5, 1)
                    },
                    new()
                    {
                        Symbol = "AAPL",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.10m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    }
                }
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>(),
            EvaluationTimeUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = engine.Evaluate(request);

        Assert.True(result.SymbolRulesEvaluated);
        Assert.Equal(1, result.SymbolMetricCount);
        Assert.Empty(result.AlertCandidates);
    }

    [Fact]
    public async Task RiskMonitoringService_HandlesDynamicSymbolsAndSavesSymbolSpecificAlerts()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreateReportSummaryWithDynamicSymbols(),
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
        Assert.True(result.SymbolMonitoringAvailable);
        Assert.Equal(3, result.SymbolCount);
        Assert.Equal(new[] { "MSFT", "NVDA", "TSLA" }, result.Symbols);

        Assert.Equal(4, result.AlertCandidateCount);
        Assert.Equal(4, result.SavedAlertCount);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Symbol)
            .ThenBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(4, alerts.Count);

        Assert.DoesNotContain(alerts, alert => alert.Symbol is null);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "MSFT" &&
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "MSFT" &&
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "NVDA" &&
            alert.Type == AlertType.StaleEquityData &&
            alert.Severity == AlertSeverity.Medium);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "TSLA" &&
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal("COMPLETED", snapshot.Status);
        Assert.True(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(3, snapshot.SymbolCount);
        Assert.Equal(4, snapshot.ActiveAlertCount);
        Assert.Equal(1, snapshot.HighAlertCount);
        Assert.Equal(3, snapshot.MediumAlertCount);
    }

    [Fact]
    public async Task RiskMonitoringService_NormalizesReturnedSymbolsInRunResult()
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
                Symbols = new List<JavaBackendSymbolRiskMetricDto>
                {
                    new()
                    {
                        Symbol = " aapl ",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.10m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    },
                    new()
                    {
                        Symbol = "msft",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.10m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    },
                    new()
                    {
                        Symbol = "MSFT",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.10m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    },
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
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.True(result.SymbolMonitoringAvailable);
        Assert.Equal(2, result.SymbolCount);
        Assert.Equal(new[] { "AAPL", "MSFT" }, result.Symbols);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.True(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(2, snapshot.SymbolCount);
    }

    [Fact]
    public async Task RiskMonitoringService_DoesNotHardCodeTickerList()
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
                Symbols = new List<JavaBackendSymbolRiskMetricDto>
                {
                    new()
                    {
                        Symbol = "SHOP",
                        SharpeRatio = 0.50m,
                        MaxDrawdown = -0.30m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    },
                    new()
                    {
                        Symbol = "RY",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.10m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    }
                }
            },
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.True(result.SymbolMonitoringAvailable);
        Assert.Equal(2, result.SymbolCount);
        Assert.Equal(new[] { "RY", "SHOP" }, result.Symbols);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Symbol)
            .ThenBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "SHOP" &&
            alert.Type == AlertType.LowSharpeRatio);

        Assert.Contains(alerts, alert =>
            alert.Symbol == "SHOP" &&
            alert.Type == AlertType.DrawdownBreach);

        Assert.DoesNotContain(alerts, alert =>
            alert.Symbol == "AAPL" ||
            alert.Symbol == "MSFT" ||
            alert.Symbol == "GOOGL" ||
            alert.Symbol == "SPY");
    }

    private static JavaBackendReportSummaryDto CreateReportSummaryWithDynamicSymbols()
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
            Symbols = new List<JavaBackendSymbolRiskMetricDto>
            {
                new()
                {
                    Symbol = "MSFT",
                    SharpeRatio = 0.75m,
                    MaxDrawdown = -0.25m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                },
                new()
                {
                    Symbol = "NVDA",
                    SharpeRatio = 1.25m,
                    MaxDrawdown = -0.10m,
                    LatestDataDate = new DateOnly(2026, 5, 1)
                },
                new()
                {
                    Symbol = "TSLA",
                    SharpeRatio = 0.80m,
                    MaxDrawdown = -0.10m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                }
            }
        };
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