using MegaFintradeRiskMonitor.Dtos.Ai;

namespace MegaFintradeRiskMonitor.Services;

public interface IAiDecisionSupportClient
{
    Task<AiIntegrationStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<AiRiskBriefResultDto> GetRiskBriefAsync(
        AiRiskBriefRequestDto request,
        CancellationToken cancellationToken = default);
}