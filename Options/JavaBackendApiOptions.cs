namespace MegaFintradeRiskMonitor.Options;

public class JavaBackendApiOptions
{
    public const string SectionName = "JavaBackendApi";

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public int TimeoutSeconds { get; set; } = 10;
}