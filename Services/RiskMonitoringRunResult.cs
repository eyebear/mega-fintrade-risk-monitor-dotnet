namespace MegaFintradeRiskMonitor.Services;

public class RiskMonitoringRunResult
{
    public DateTime StartedAtUtc { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public bool JavaBackendReachable { get; set; }

    public bool ReportSummaryAvailable { get; set; }

    public bool ImportAuditAvailable { get; set; }

    public bool ImportRejectionsAvailable { get; set; }

    public bool PortfolioMonitoringAvailable { get; set; }

    public bool SymbolMonitoringAvailable { get; set; }

    public int SymbolCount { get; set; }

    public IReadOnlyList<string> Symbols { get; set; } = Array.Empty<string>();

    public int AlertCandidateCount { get; set; }

    public int SavedAlertCount { get; set; }

    public long MonitoringSnapshotId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}