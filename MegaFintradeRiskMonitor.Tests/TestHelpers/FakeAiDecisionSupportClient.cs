using MegaFintradeRiskMonitor.Dtos.Ai;
using MegaFintradeRiskMonitor.Services;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public class FakeAiDecisionSupportClient : IAiDecisionSupportClient
{
    public int GetStatusCallCount { get; private set; }

    public int GetRiskBriefCallCount { get; private set; }

    public AiIntegrationStatusDto StatusToReturn { get; set; } = new()
    {
        Enabled = false,
        Mode = "DISABLED",
        Project5BaseUrl = "http://localhost:7005",
        AdvisorReachable = false,
        Status = "AI_DISABLED",
        Message = "AI integration is disabled in tests.",
        ProviderSelectionOwnedByAdvisor = true,
        ApiTokensStoredInRiskMonitor = false,
        CheckedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
    };

    public AiRiskBriefResultDto RiskBriefToReturn { get; set; } = new()
    {
        Available = false,
        Status = "AI_DISABLED",
        Summary = "AI risk brief unavailable in tests.",
        RecommendedAction = "Use deterministic monitor data.",
        KeyRisks = Array.Empty<string>(),
        SymbolHighlights = Array.Empty<string>(),
        GeneratedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
    };

    public Task<AiIntegrationStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        GetStatusCallCount++;

        return Task.FromResult(StatusToReturn);
    }

    public Task<AiRiskBriefResultDto> GetRiskBriefAsync(
        AiRiskBriefRequestDto request,
        CancellationToken cancellationToken = default)
    {
        GetRiskBriefCallCount++;

        return Task.FromResult(RiskBriefToReturn);
    }
}