namespace MegaFintradeRiskMonitor.Dtos.Monitor;

public class SymbolRiskStatusDto
{
    public string Symbol { get; set; } = string.Empty;

    public decimal? SharpeRatio { get; set; }

    public decimal? MaxDrawdown { get; set; }

    public DateOnly? LatestDataDate { get; set; }

    public bool HasSharpeRatio => SharpeRatio.HasValue;

    public bool HasMaxDrawdown => MaxDrawdown.HasValue;

    public bool HasLatestDataDate => LatestDataDate.HasValue;
}