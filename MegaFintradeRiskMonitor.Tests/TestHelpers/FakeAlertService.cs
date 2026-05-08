using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Services;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public class FakeAlertService : IAlertService
{
    public List<RiskAlert> Alerts { get; } = new();

    public IReadOnlyList<RiskAlertCandidate> LastSavedCandidates { get; private set; } =
        Array.Empty<RiskAlertCandidate>();

    public Task<IReadOnlyList<RiskAlert>> GetAllAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var alerts = ApplySymbolFilter(Alerts, symbol)
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<RiskAlert>>(alerts);
    }

    public Task<IReadOnlyList<RiskAlert>> GetActiveAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var alerts = ApplySymbolFilter(Alerts, symbol)
            .Where(alert => alert.IsActive)
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.CreatedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<RiskAlert>>(alerts);
    }

    public Task<IReadOnlyList<RiskAlert>> GetAlertHistoryAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var alerts = ApplySymbolFilter(Alerts, symbol)
            .Where(alert => !alert.IsActive || alert.ResolvedAtUtc.HasValue)
            .OrderByDescending(alert => alert.ResolvedAtUtc ?? alert.CreatedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<RiskAlert>>(alerts);
    }

    public Task<RiskAlert?> GetAlertByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = Alerts.FirstOrDefault(existingAlert => existingAlert.Id == id);

        return Task.FromResult(alert);
    }

    public Task<IReadOnlyList<RiskAlert>> SaveAlertCandidatesAsync(
        IReadOnlyList<RiskAlertCandidate> alertCandidates,
        CancellationToken cancellationToken = default)
    {
        LastSavedCandidates = alertCandidates;

        var savedAlerts = alertCandidates
            .Select((candidate, index) => new RiskAlert
            {
                Id = Alerts.Count + index + 1,
                Symbol = NormalizeNullableSymbol(candidate.Symbol),
                Type = candidate.Type,
                Severity = candidate.Severity,
                Message = candidate.Message,
                SourceEndpoint = candidate.SourceEndpoint,
                SourceValue = candidate.SourceValue,
                ThresholdValue = candidate.ThresholdValue,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        Alerts.AddRange(savedAlerts);

        return Task.FromResult<IReadOnlyList<RiskAlert>>(savedAlerts);
    }

    public Task<RiskAlert?> ResolveAlertAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = Alerts.FirstOrDefault(existingAlert => existingAlert.Id == id);

        if (alert is null)
        {
            return Task.FromResult<RiskAlert?>(null);
        }

        alert.IsActive = false;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        return Task.FromResult<RiskAlert?>(alert);
    }

    public Task<bool> DeleteAlertAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = Alerts.FirstOrDefault(existingAlert => existingAlert.Id == id);

        if (alert is null)
        {
            return Task.FromResult(false);
        }

        Alerts.Remove(alert);

        return Task.FromResult(true);
    }

    private static IEnumerable<RiskAlert> ApplySymbolFilter(
        IEnumerable<RiskAlert> alerts,
        string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return alerts;
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        return alerts.Where(alert =>
            string.Equals(
                alert.Symbol,
                normalizedSymbol,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeNullableSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        return symbol.Trim().ToUpperInvariant();
    }
}