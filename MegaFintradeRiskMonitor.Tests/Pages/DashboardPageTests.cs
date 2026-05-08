using MegaFintradeRiskMonitor.Dtos.Project1;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Pages;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Pages;

public class DashboardPageTests
{
    [Fact]
    public async Task OnGetAsync_LoadsDashboardData_WhenJavaBackendReachable()
    {
        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreatePortfolioSummaryWithSymbols(),
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

        var alertService = new FakeAlertService();

        alertService.Alerts.Add(
            CreateAlert(
                id: 1,
                symbol: "MSFT",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: true));

        var monitoringService = new FakeRiskMonitoringService();
        var aiClient = new FakeAiDecisionSupportClient();

        var pageModel = CreateDashboardModel(
            javaBackendClient,
            alertService,
            monitoringService,
            aiClient);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.True(pageModel.JavaBackendReachable);
        Assert.Equal("http://localhost:8080", pageModel.JavaBackendBaseUrl);
        Assert.Equal(10, pageModel.JavaBackendTimeoutSeconds);
        Assert.NotNull(pageModel.PortfolioReportSummary);
        Assert.True(pageModel.PortfolioReportAvailable);
        Assert.Equal(2, pageModel.SymbolMetrics.Count);
        Assert.Single(pageModel.ActiveAlerts);
        Assert.Single(pageModel.ImportAudits);
        Assert.NotNull(pageModel.LatestImportAudit);
        Assert.Empty(pageModel.ImportRejections);
        Assert.Null(pageModel.LatestImportRejection);
        Assert.NotNull(pageModel.AiIntegrationStatus);
        Assert.Equal("AI_DISABLED", pageModel.AiIntegrationStatus.Status);
        Assert.Equal(1, aiClient.GetStatusCallCount);
    }

    [Fact]
    public async Task OnGetAsync_LoadsGracefully_WhenJavaBackendUnavailable()
    {
        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = false,
            ReportSummary = CreatePortfolioSummaryWithSymbols(),
            ImportAudits = new List<JavaBackendImportAuditDto>
            {
                new()
                {
                    Id = 1,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    Status = "COMPLETED"
                }
            },
            ImportRejections = new List<JavaBackendImportRejectionDto>
            {
                new()
                {
                    Id = 1,
                    ImportType = "RISK_METRICS",
                    SourceFile = "risk_metrics.csv",
                    RowNumber = 5,
                    Reason = "Invalid value"
                }
            }
        };

        var alertService = new FakeAlertService();
        var monitoringService = new FakeRiskMonitoringService();
        var aiClient = new FakeAiDecisionSupportClient();

        var pageModel = CreateDashboardModel(
            javaBackendClient,
            alertService,
            monitoringService,
            aiClient);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.False(pageModel.JavaBackendReachable);
        Assert.Null(pageModel.PortfolioReportSummary);
        Assert.False(pageModel.PortfolioReportAvailable);
        Assert.Empty(pageModel.SymbolMetrics);
        Assert.Empty(pageModel.ImportAudits);
        Assert.Empty(pageModel.ImportRejections);
        Assert.Null(pageModel.LatestImportAudit);
        Assert.Null(pageModel.LatestImportRejection);
        Assert.NotNull(pageModel.AiIntegrationStatus);

        Assert.Equal(1, javaBackendClient.ReachabilityCallCount);
        Assert.Equal(0, javaBackendClient.ReportSummaryCallCount);
        Assert.Equal(0, javaBackendClient.ImportAuditCallCount);
        Assert.Equal(0, javaBackendClient.ImportRejectionsCallCount);
    }

    [Fact]
    public async Task OnGetAsync_AppliesSymbolFilter_ToSymbolsAndAlerts()
    {
        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreatePortfolioSummaryWithSymbols(),
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var alertService = new FakeAlertService();

        alertService.Alerts.AddRange(
            CreateAlert(
                id: 1,
                symbol: "AAPL",
                type: AlertType.DrawdownBreach,
                severity: AlertSeverity.High,
                isActive: true),
            CreateAlert(
                id: 2,
                symbol: "MSFT",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: true));

        var pageModel = CreateDashboardModel(
            javaBackendClient,
            alertService,
            new FakeRiskMonitoringService(),
            new FakeAiDecisionSupportClient());

        pageModel.SymbolFilter = "msft";

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.Single(pageModel.SymbolMetrics);
        Assert.Equal("MSFT", pageModel.SymbolMetrics[0].Symbol);

        Assert.Single(pageModel.ActiveAlerts);
        Assert.Equal("MSFT", pageModel.ActiveAlerts[0].Symbol);
    }

    [Fact]
    public async Task OnPostRunMonitorAsync_RunsMonitorAndReloadsDashboardData()
    {
        var javaBackendClient = new FakeJavaBackendApiClient
        {
            BackendReachable = true,
            ReportSummary = CreatePortfolioSummaryWithSymbols(),
            ImportAudits = Array.Empty<JavaBackendImportAuditDto>(),
            ImportRejections = Array.Empty<JavaBackendImportRejectionDto>()
        };

        var monitoringService = new FakeRiskMonitoringService
        {
            ResultToReturn = new()
            {
                StartedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
                CompletedAtUtc = new DateTime(2026, 5, 7, 12, 0, 1, DateTimeKind.Utc),
                JavaBackendReachable = true,
                ReportSummaryAvailable = true,
                ImportAuditAvailable = false,
                ImportRejectionsAvailable = false,
                PortfolioMonitoringAvailable = true,
                SymbolMonitoringAvailable = true,
                SymbolCount = 2,
                Symbols = new[] { "AAPL", "MSFT" },
                AlertCandidateCount = 2,
                SavedAlertCount = 2,
                MonitoringSnapshotId = 99,
                Status = "COMPLETED",
                Message = "Manual test run completed."
            }
        };

        var pageModel = CreateDashboardModel(
            javaBackendClient,
            new FakeAlertService(),
            monitoringService,
            new FakeAiDecisionSupportClient());

        var result = await pageModel.OnPostRunMonitorAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(pageModel.ManualRunResult);
        Assert.Equal("COMPLETED", pageModel.ManualRunResult.Status);
        Assert.Equal(99, pageModel.ManualRunResult.MonitoringSnapshotId);
        Assert.Equal(1, monitoringService.RunOnceCallCount);
        Assert.True(pageModel.JavaBackendReachable);
        Assert.NotNull(pageModel.PortfolioReportSummary);
    }

    [Fact]
    public void GetSymbolAlertStatus_ReturnsExpectedStatus()
    {
        var pageModel = CreateDashboardModel(
            new FakeJavaBackendApiClient(),
            new FakeAlertService(),
            new FakeRiskMonitoringService(),
            new FakeAiDecisionSupportClient());

        var activeAlertsProperty = typeof(DashboardModel)
            .GetProperty(nameof(DashboardModel.ActiveAlerts));

        Assert.NotNull(activeAlertsProperty);

        activeAlertsProperty.SetValue(
            pageModel,
            new List<RiskAlert>
            {
                CreateAlert(
                    id: 1,
                    symbol: "AAPL",
                    type: AlertType.DrawdownBreach,
                    severity: AlertSeverity.High,
                    isActive: true),
                CreateAlert(
                    id: 2,
                    symbol: "MSFT",
                    type: AlertType.LowSharpeRatio,
                    severity: AlertSeverity.Medium,
                    isActive: true)
            });

        Assert.Equal("Drawdown Breach", pageModel.GetSymbolAlertStatus("AAPL"));
        Assert.Equal("Low Sharpe", pageModel.GetSymbolAlertStatus("MSFT"));
        Assert.Equal("Normal", pageModel.GetSymbolAlertStatus("NVDA"));
    }

    private static DashboardModel CreateDashboardModel(
        FakeJavaBackendApiClient javaBackendClient,
        FakeAlertService alertService,
        FakeRiskMonitoringService monitoringService,
        FakeAiDecisionSupportClient aiClient)
    {
        return new DashboardModel(
            javaBackendClient,
            alertService,
            monitoringService,
            aiClient,
        Microsoft.Extensions.Options.Options.Create(new JavaBackendApiOptions
        {
            BaseUrl = "http://localhost:8080",
            TimeoutSeconds = 10
        }),
        Microsoft.Extensions.Options.Options.Create(new AiIntegrationOptions
        {
            Enabled = false,
            Project5BaseUrl = "http://localhost:7005"
        }));
    }

    private static JavaBackendReportSummaryDto CreatePortfolioSummaryWithSymbols()
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
                    Symbol = "AAPL",
                    SharpeRatio = 1.25m,
                    MaxDrawdown = -0.12m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                },
                new()
                {
                    Symbol = "MSFT",
                    SharpeRatio = 0.82m,
                    MaxDrawdown = -0.18m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                }
            }
        };
    }

    private static RiskAlert CreateAlert(
        long id,
        string? symbol,
        AlertType type,
        AlertSeverity severity,
        bool isActive)
    {
        return new RiskAlert
        {
            Id = id,
            Symbol = symbol,
            Type = type,
            Severity = severity,
            Message = $"Test alert for {type}",
            SourceEndpoint = "/api/reports/summary",
            SourceValue = "test-source-value",
            ThresholdValue = "test-threshold-value",
            IsActive = isActive,
            CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}