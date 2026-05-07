namespace MegaFintradeRiskMonitor.Dtos.Project1;

public class JavaBackendReportSummaryDto
{
    public decimal? PortfolioSharpeRatio { get; set; }

    public decimal? PortfolioMaxDrawdown { get; set; }

    public DateOnly? LatestEquityDate { get; set; }

    public int? RiskMetricRowCount { get; set; }

    public int? BacktestResultRowCount { get; set; }

    public int? StrategySignalRowCount { get; set; }

    public int? EquityCurveRowCount { get; set; }

    public List<JavaBackendSymbolRiskMetricDto> Symbols { get; set; } = new();
}