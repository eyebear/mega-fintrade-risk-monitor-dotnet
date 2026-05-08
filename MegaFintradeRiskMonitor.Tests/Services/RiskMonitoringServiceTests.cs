using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class RiskMonitoringServiceTests
{
    [Fact]
    public async Task RunOnceAsync_SavesSnapshotAndReturnsCompletedResult_WhenJavaBackendReachable()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreateHealthyPortfolioSummary(),
            ImportAudits = new List<JavaBackendImportAuditDto>
            {
                new()
                {
                    Id = 1,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    Status = "COMPLETED",
                    TotalRows = 4,
                    RejectedRows = 0,
                    StartedAtUtc = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
                    CompletedAtUtc = new DateTime(2026, 5, 7, 10, 1, 0, DateTimeKind.Utc)
                }
            },
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.Equal("COMPLETED", result.Status);
        Assert.True(result.JavaBackendReachable);
        Assert.True(result.ReportSummaryAvailable);
        Assert.True(result.ImportAuditAvailable);
        Assert.False(result.ImportRejectionsAvailable);
        Assert.True(result.PortfolioMonitoringAvailable);
        Assert.False(result.SymbolMonitoringAvailable);
        Assert.Equal(0, result.SymbolCount);
        Assert.Empty(result.Symbols);
        Assert.True(result.MonitoringSnapshotId > 0);

        Assert.Equal(1, javaBackendClient.ReachabilityCallCount);
        Assert.Equal(1, javaBackendClient.ReportSummaryCallCount);
        Assert.Equal(1, javaBackendClient.ImportAuditCallCount);
        Assert.Equal(1, javaBackendClient.ImportRejectionsCallCount);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal(result.MonitoringSnapshotId, snapshot.Id);
        Assert.Equal("COMPLETED", snapshot.Status);
        Assert.True(snapshot.JavaBackendReachable);
        Assert.True(snapshot.ReportSummaryAvailable);
        Assert.True(snapshot.ImportAuditAvailable);
        Assert.False(snapshot.ImportRejectionsAvailable);
        Assert.True(snapshot.PortfolioMonitoringAvailable);
        Assert.False(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(0, snapshot.SymbolCount);
    }

    [Fact]
    public async Task RunOnceAsync_CreatesUnavailableAlertAndSnapshot_WhenJavaBackendUnavailable()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = false,
            ReportSummary = null,
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.Equal("JAVA_BACKEND_UNAVAILABLE", result.Status);
        Assert.False(result.JavaBackendReachable);
        Assert.False(result.ReportSummaryAvailable);
        Assert.False(result.ImportAuditAvailable);
        Assert.False(result.ImportRejectionsAvailable);
        Assert.False(result.PortfolioMonitoringAvailable);
        Assert.False(result.SymbolMonitoringAvailable);
        Assert.Equal(0, result.SymbolCount);
        Assert.Equal(1, result.AlertCandidateCount);
        Assert.Equal(1, result.SavedAlertCount);

        Assert.Equal(1, javaBackendClient.ReachabilityCallCount);
        Assert.Equal(0, javaBackendClient.ReportSummaryCallCount);
        Assert.Equal(0, javaBackendClient.ImportAuditCallCount);
        Assert.Equal(0, javaBackendClient.ImportRejectionsCallCount);

        var savedAlert = await dbContext.RiskAlerts.SingleAsync();

        Assert.Null(savedAlert.Symbol);
        Assert.Equal(AlertType.JavaBackendUnavailable, savedAlert.Type);
        Assert.Equal(AlertSeverity.Critical, savedAlert.Severity);
        Assert.True(savedAlert.IsActive);
        Assert.Equal("JavaBackendApi", savedAlert.SourceEndpoint);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal("JAVA_BACKEND_UNAVAILABLE", snapshot.Status);
        Assert.False(snapshot.JavaBackendReachable);
        Assert.Equal(1, snapshot.ActiveAlertCount);
        Assert.Equal(1, snapshot.CriticalAlertCount);
    }

    [Fact]
    public async Task RunOnceAsync_DoesNotDuplicateUnavailableAlert_OnRepeatedRuns()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = false
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var firstRun = await service.RunOnceAsync();
        var secondRun = await service.RunOnceAsync();

        Assert.Equal(1, firstRun.SavedAlertCount);
        Assert.Equal(0, secondRun.SavedAlertCount);
        Assert.Equal(1, await dbContext.RiskAlerts.CountAsync());
        Assert.Equal(2, await dbContext.MonitoringSnapshots.CountAsync());
    }

    [Fact]
    public async Task RunOnceAsync_SavesPortfolioAlerts_WhenPortfolioMetricsBreachThresholds()
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
        Assert.True(result.JavaBackendReachable);
        Assert.Equal(2, result.AlertCandidateCount);
        Assert.Equal(2, result.SavedAlertCount);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);

        Assert.Contains(alerts, alert =>
            alert.Symbol is null &&
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(alerts, alert =>
            alert.Symbol is null &&
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.Equal(2, snapshot.ActiveAlertCount);
        Assert.Equal(1, snapshot.HighAlertCount);
        Assert.Equal(1, snapshot.MediumAlertCount);
    }

    [Fact]
    public async Task RunOnceAsync_HandlesPortfolioOnlyBackendResponse()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreateHealthyPortfolioSummary(),
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
    public async Task RunOnceAsync_HandlesDynamicSymbolLevelBackendResponse()
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
                        Symbol = "AAPL",
                        SharpeRatio = 1.25m,
                        MaxDrawdown = -0.12m,
                        LatestDataDate = new DateOnly(2026, 5, 7)
                    },
                    new()
                    {
                        Symbol = "MSFT",
                        SharpeRatio = 0.75m,
                        MaxDrawdown = -0.25m,
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

        Assert.Equal("COMPLETED", result.Status);
        Assert.True(result.SymbolMonitoringAvailable);
        Assert.Equal(2, result.SymbolCount);
        Assert.Equal(new[] { "AAPL", "MSFT" }, result.Symbols);
        Assert.Equal(2, result.AlertCandidateCount);
        Assert.Equal(2, result.SavedAlertCount);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);

        Assert.All(alerts, alert => Assert.Equal("MSFT", alert.Symbol));

        Assert.Contains(alerts, alert =>
            alert.Type == AlertType.DrawdownBreach &&
            alert.Severity == AlertSeverity.High);

        Assert.Contains(alerts, alert =>
            alert.Type == AlertType.LowSharpeRatio &&
            alert.Severity == AlertSeverity.Medium);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.True(snapshot.SymbolMonitoringAvailable);
        Assert.Equal(2, snapshot.SymbolCount);
        Assert.Equal(2, snapshot.ActiveAlertCount);
    }

    [Fact]
    public async Task RunOnceAsync_SavesCsvRejectionAlert_WhenRejectionsExist()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreateHealthyPortfolioSummary(),
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = new List<JavaBackendImportRejectionDto>
            {
                new()
                {
                    Id = 1,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    RowNumber = 10,
                    Reason = "Invalid decimal value",
                    CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        var service = CreateRiskMonitoringService(
            javaBackendClient,
            dbContext);

        var result = await service.RunOnceAsync();

        Assert.True(result.ImportRejectionsAvailable);
        Assert.Equal(1, result.AlertCandidateCount);
        Assert.Equal(1, result.SavedAlertCount);

        var alert = await dbContext.RiskAlerts.SingleAsync();

        Assert.Equal(AlertType.CsvRejectionsFound, alert.Type);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);

        var snapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.True(snapshot.ImportRejectionsAvailable);
        Assert.Equal(1, snapshot.ActiveAlertCount);
    }

    private static RiskMonitoringService CreateRiskMonitoringService(
        FakeJavaBackendApiClient javaBackendClient,
        RiskMonitorDbContext dbContext)
    {
        var alertRuleEngine = new AlertRuleEngine(
            Microsoft.Extensions.Options.Options.Create(
                new AlertRuleOptions
                {
                    MaxDrawdownThreshold = -0.20m,
                    MinimumSharpeRatio = 1.0m,
                    StaleDataDays = 3,
                    CsvRejectionThreshold = 0
                }));

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