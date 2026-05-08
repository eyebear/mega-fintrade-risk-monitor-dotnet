using MegaFintradeRiskMonitor.Services;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public class FakeRiskMonitoringService : IRiskMonitoringService
{
    public int RunOnceCallCount { get; private set; }

    public RiskMonitoringRunResult ResultToReturn { get; set; } = new()
    {
        StartedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
        CompletedAtUtc = new DateTime(2026, 5, 7, 12, 0, 1, DateTimeKind.Utc),
        JavaBackendReachable = true,
        ReportSummaryAvailable = true,
        ImportAuditAvailable = true,
        ImportRejectionsAvailable = false,
        PortfolioMonitoringAvailable = true,
        SymbolMonitoringAvailable = false,
        SymbolCount = 0,
        Symbols = Array.Empty<string>(),
        AlertCandidateCount = 0,
        SavedAlertCount = 0,
        MonitoringSnapshotId = 1,
        Status = "COMPLETED",
        Message = "Fake monitoring run completed."
    };

    public Task<RiskMonitoringRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        RunOnceCallCount++;

        return Task.FromResult(ResultToReturn);
    }
}