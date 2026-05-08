using MegaFintradeRiskMonitor.Models;

namespace MegaFintradeRiskMonitor.Services;

public class RiskAlertCandidate
{
    public string? Symbol { get; set; }

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Message { get; set; } = string.Empty;

    public string SourceEndpoint { get; set; } = string.Empty;

    public string? SourceValue { get; set; }

    public string? ThresholdValue { get; set; }

    public RiskAlert ToRiskAlert()
    {
        return new RiskAlert
        {
            Symbol = Symbol,
            Type = Type,
            Severity = Severity,
            Message = Message,
            SourceEndpoint = SourceEndpoint,
            SourceValue = SourceValue,
            ThresholdValue = ThresholdValue,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}