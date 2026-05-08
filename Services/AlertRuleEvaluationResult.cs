namespace MegaFintradeRiskMonitor.Services;

public class AlertRuleEvaluationResult
{
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool JavaBackendReachable { get; set; }

    public bool PortfolioRulesEvaluated { get; set; }

    public bool SymbolRulesEvaluated { get; set; }

    public int SymbolMetricCount { get; set; }

    public int AlertCandidateCount => AlertCandidates.Count;

    public int SystemAlertCandidateCount =>
        AlertCandidates.Count(alert => string.IsNullOrWhiteSpace(alert.Symbol));

    public int SymbolAlertCandidateCount =>
        AlertCandidates.Count(alert => !string.IsNullOrWhiteSpace(alert.Symbol));

    public IReadOnlyList<RiskAlertCandidate> AlertCandidates { get; set; } =
        Array.Empty<RiskAlertCandidate>();
}