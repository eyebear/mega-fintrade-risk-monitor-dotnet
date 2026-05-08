namespace MegaFintradeRiskMonitor.Dtos.Ai;

public class AiRiskBriefRequestDto
{
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public AiPortfolioRiskSnapshotDto? Portfolio { get; set; }

    public IReadOnlyList<AiSymbolRiskSnapshotDto> Symbols { get; set; } =
        Array.Empty<AiSymbolRiskSnapshotDto>();

    public IReadOnlyList<AiAlertSnapshotDto> ActiveAlerts { get; set; } =
        Array.Empty<AiAlertSnapshotDto>();
}

public class AiPortfolioRiskSnapshotDto
{
    public decimal? SharpeRatio { get; set; }

    public decimal? MaxDrawdown { get; set; }

    public DateOnly? LatestEquityDate { get; set; }
}

public class AiSymbolRiskSnapshotDto
{
    public string Symbol { get; set; } = string.Empty;

    public decimal? SharpeRatio { get; set; }

    public decimal? MaxDrawdown { get; set; }

    public DateOnly? LatestDataDate { get; set; }
}

public class AiAlertSnapshotDto
{
    public string? Symbol { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string SourceEndpoint { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}