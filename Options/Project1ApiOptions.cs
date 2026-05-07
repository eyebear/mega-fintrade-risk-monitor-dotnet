namespace MegaFintradeRiskMonitor.Options;

public class Project1ApiOptions
{
    public const string SectionName = "Project1Api";

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public int TimeoutSeconds { get; set; } = 10;
}