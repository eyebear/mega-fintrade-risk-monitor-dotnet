namespace MegaFintradeRiskMonitor.Dtos.Ai;

public class AiRiskBriefResultDto
{
    public bool Available { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string RecommendedAction { get; set; } = string.Empty;

    public IReadOnlyList<string> KeyRisks { get; set; } =
        Array.Empty<string>();

    public IReadOnlyList<string> SymbolHighlights { get; set; } =
        Array.Empty<string>();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}