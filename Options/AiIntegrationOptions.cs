namespace MegaFintradeRiskMonitor.Options;

public class AiIntegrationOptions
{
    public const string SectionName = "AiIntegration";

    public bool Enabled { get; set; } = false;

    public string Project5BaseUrl { get; set; } = "http://localhost:7005";
}