namespace MegaFintradeRiskMonitor.Options;

public class AlertRuleOptions
{
    public const string SectionName = "AlertRules";

    public decimal MaxDrawdownThreshold { get; set; } = -0.20m;

    public decimal MinimumSharpeRatio { get; set; } = 1.0m;

    public int StaleDataDays { get; set; } = 3;

    public int CsvRejectionThreshold { get; set; } = 0;
}