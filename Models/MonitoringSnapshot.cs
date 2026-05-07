namespace MegaFintradeRiskMonitor.Models;

public class MonitoringSnapshot
{
    public long Id { get; set; }

    public bool JavaBackendReachable { get; set; }

    public string JavaBackendBaseUrl { get; set; } = string.Empty;

    public bool ReportSummaryAvailable { get; set; }

    public bool ImportAuditAvailable { get; set; }

    public bool ImportRejectionsAvailable { get; set; }

    public bool PortfolioMonitoringAvailable { get; set; }

    public bool SymbolMonitoringAvailable { get; set; }

    public int SymbolCount { get; set; }

    public int ActiveAlertCount { get; set; }

    public int CriticalAlertCount { get; set; }

    public int HighAlertCount { get; set; }

    public int MediumAlertCount { get; set; }

    public int LowAlertCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}