namespace MegaFintradeRiskMonitor.Dtos.Project1;

public class JavaBackendSymbolRiskMetricDto
{
    public string Symbol { get; set; } = string.Empty;

    public decimal? SharpeRatio { get; set; }

    public decimal? MaxDrawdown { get; set; }

    public DateOnly? LatestDataDate { get; set; }
}