namespace MegaFintradeRiskMonitor.Models;

public class RiskAlert
{
    public long Id { get; set; }

    public string? Symbol { get; set; }

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Message { get; set; } = string.Empty;

    public string SourceEndpoint { get; set; } = string.Empty;

    public string? SourceValue { get; set; }

    public string? ThresholdValue { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAtUtc { get; set; }
}