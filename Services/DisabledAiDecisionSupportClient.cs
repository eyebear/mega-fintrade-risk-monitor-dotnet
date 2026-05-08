using MegaFintradeRiskMonitor.Dtos.Ai;
using MegaFintradeRiskMonitor.Options;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Services;

public class DisabledAiDecisionSupportClient : IAiDecisionSupportClient
{
    private readonly AiIntegrationOptions _aiIntegrationOptions;

    public DisabledAiDecisionSupportClient(
        IOptions<AiIntegrationOptions> aiIntegrationOptions)
    {
        _aiIntegrationOptions = aiIntegrationOptions.Value;
    }

    public Task<AiIntegrationStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var status = new AiIntegrationStatusDto
        {
            Enabled = _aiIntegrationOptions.Enabled,
            Project5BaseUrl = _aiIntegrationOptions.Project5BaseUrl,
            AdvisorReachable = false,
            Mode = _aiIntegrationOptions.Enabled
                ? "PLACEHOLDER_CLIENT_CONFIGURED"
                : "DISABLED",
            Status = _aiIntegrationOptions.Enabled
                ? "AI_ADVISOR_NOT_CONNECTED"
                : "AI_DISABLED",
            Message = _aiIntegrationOptions.Enabled
                ? "AI integration is enabled in configuration, but no AI advisor client has been implemented yet. Core monitoring remains fully functional."
                : "AI integration is disabled. This is expected until the future AI advisor service is added.",
            ProviderSelectionOwnedByAdvisor = true,
            ApiTokensStoredInRiskMonitor = false,
            CheckedAtUtc = DateTime.UtcNow
        };

        return Task.FromResult(status);
    }

    public Task<AiRiskBriefResultDto> GetRiskBriefAsync(
        AiRiskBriefRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = new AiRiskBriefResultDto
        {
            Available = false,
            Status = _aiIntegrationOptions.Enabled
                ? "AI_ADVISOR_NOT_CONNECTED"
                : "AI_DISABLED",
            Summary = "AI risk brief is not available yet. Core deterministic monitoring, alerting, and dashboard features continue to work without AI.",
            RecommendedAction = "Use portfolio metrics, symbol metrics, import health, rejection health, and active alerts from the deterministic monitor.",
            KeyRisks = Array.Empty<string>(),
            SymbolHighlights = Array.Empty<string>(),
            GeneratedAtUtc = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }
}