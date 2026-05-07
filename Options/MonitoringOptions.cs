namespace MegaFintradeRiskMonitor.Options;

public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int PollingIntervalSeconds { get; set; } = 60;
}