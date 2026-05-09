using MegaFintradeRiskMonitor.Models;

namespace MegaFintradeRiskMonitor.Services;

public interface IAlertService
{
    Task<IReadOnlyList<RiskAlert>> GetAllAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAlert>> GetActiveAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAlert>> GetAlertHistoryAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default);

    Task<RiskAlert?> GetAlertByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAlert>> SaveAlertCandidatesAsync(
        IReadOnlyList<RiskAlertCandidate> alertCandidates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAlert>> ResolveStaleAlertsAsync(
        IReadOnlyList<RiskAlertCandidate> currentAlertCandidates,
        CancellationToken cancellationToken = default);

    Task<RiskAlert?> ResolveAlertAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAlertAsync(
        long id,
        CancellationToken cancellationToken = default);
}