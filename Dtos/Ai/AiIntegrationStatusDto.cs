namespace MegaFintradeRiskMonitor.Dtos.Ai;

public class AiIntegrationStatusDto
{
    public bool Enabled { get; set; }

    public string Mode { get; set; } = string.Empty;

    public string Project5BaseUrl { get; set; } = string.Empty;

    public bool AdvisorReachable { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool ProviderSelectionOwnedByAdvisor { get; set; } = true;

    public bool ApiTokensStoredInRiskMonitor { get; set; } = false;

    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}