using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaFintradeRiskMonitor.Services;

public class AlertService : IAlertService
{
    private readonly RiskMonitorDbContext _dbContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        RiskMonitorDbContext dbContext,
        ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RiskAlert>> GetAllAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RiskAlerts
            .AsNoTracking()
            .AsQueryable();

        query = ApplySymbolFilter(query, symbol);

        return await query
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RiskAlert>> GetActiveAlertsAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RiskAlerts
            .AsNoTracking()
            .Where(alert => alert.IsActive);

        query = ApplySymbolFilter(query, symbol);

        return await query
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RiskAlert>> GetAlertHistoryAsync(
        string? symbol = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RiskAlerts
            .AsNoTracking()
            .Where(alert => !alert.IsActive || alert.ResolvedAtUtc.HasValue);

        query = ApplySymbolFilter(query, symbol);

        return await query
            .OrderByDescending(alert => alert.ResolvedAtUtc ?? alert.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<RiskAlert?> GetAlertByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RiskAlerts
            .AsNoTracking()
            .FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RiskAlert>> SaveAlertCandidatesAsync(
        IReadOnlyList<RiskAlertCandidate> alertCandidates,
        CancellationToken cancellationToken = default)
    {
        if (alertCandidates.Count == 0)
        {
            return Array.Empty<RiskAlert>();
        }

        var savedAlerts = new List<RiskAlert>();

        foreach (var candidate in alertCandidates)
        {
            var alert = candidate.ToRiskAlert();
            NormalizeAlert(alert);

            var duplicateExists = await ActiveDuplicateExistsAsync(
                alert,
                cancellationToken);

            if (duplicateExists)
            {
                _logger.LogInformation(
                    "Skipped duplicate active alert. Type={AlertType}, Symbol={Symbol}, SourceEndpoint={SourceEndpoint}",
                    alert.Type,
                    alert.Symbol ?? "PORTFOLIO_OR_SYSTEM",
                    alert.SourceEndpoint);

                continue;
            }

            _dbContext.RiskAlerts.Add(alert);
            savedAlerts.Add(alert);
        }

        if (savedAlerts.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Saved {SavedAlertCount} new alert(s) from {CandidateCount} candidate(s).",
            savedAlerts.Count,
            alertCandidates.Count);

        return savedAlerts;
    }

    public async Task<IReadOnlyList<RiskAlert>> ResolveStaleAlertsAsync(
        IReadOnlyList<RiskAlertCandidate> currentAlertCandidates,
        CancellationToken cancellationToken = default)
    {
        var activeAlerts = await _dbContext.RiskAlerts
            .Where(alert => alert.IsActive)
            .ToListAsync(cancellationToken);

        if (activeAlerts.Count == 0)
        {
            return Array.Empty<RiskAlert>();
        }

        var currentAlertKeys = currentAlertCandidates
            .Select(CreateCandidateKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var resolvedAlerts = new List<RiskAlert>();
        var resolvedAtUtc = DateTime.UtcNow;

        foreach (var activeAlert in activeAlerts)
        {
            var activeAlertKey = CreateAlertKey(activeAlert);

            if (currentAlertKeys.Contains(activeAlertKey))
            {
                continue;
            }

            activeAlert.IsActive = false;
            activeAlert.ResolvedAtUtc = resolvedAtUtc;
            resolvedAlerts.Add(activeAlert);
        }

        if (resolvedAlerts.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Resolved {ResolvedAlertCount} stale active alert(s). CurrentCandidateCount={CurrentCandidateCount}.",
            resolvedAlerts.Count,
            currentAlertCandidates.Count);

        return resolvedAlerts;
    }

    public async Task<RiskAlert?> ResolveAlertAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.RiskAlerts
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        if (!alert.IsActive && alert.ResolvedAtUtc.HasValue)
        {
            return alert;
        }

        alert.IsActive = false;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return alert;
    }

    public async Task<bool> DeleteAlertAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.RiskAlerts
            .FirstOrDefaultAsync(existingAlert => existingAlert.Id == id, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        _dbContext.RiskAlerts.Remove(alert);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> ActiveDuplicateExistsAsync(
        RiskAlert alert,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RiskAlerts.AnyAsync(
            existingAlert =>
                existingAlert.IsActive &&
                existingAlert.Type == alert.Type &&
                existingAlert.Symbol == alert.Symbol &&
                existingAlert.SourceEndpoint == alert.SourceEndpoint,
            cancellationToken);
    }

    private static IQueryable<RiskAlert> ApplySymbolFilter(
        IQueryable<RiskAlert> query,
        string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return query;
        }

        var normalizedSymbol = NormalizeSymbol(symbol);

        return query.Where(alert =>
            alert.Symbol != null &&
            alert.Symbol.ToUpper() == normalizedSymbol);
    }

    private static void NormalizeAlert(RiskAlert alert)
    {
        alert.Symbol = NormalizeNullableSymbol(alert.Symbol);
        alert.Message = alert.Message.Trim();
        alert.SourceEndpoint = alert.SourceEndpoint.Trim();

        if (!string.IsNullOrWhiteSpace(alert.SourceValue))
        {
            alert.SourceValue = alert.SourceValue.Trim();
        }

        if (!string.IsNullOrWhiteSpace(alert.ThresholdValue))
        {
            alert.ThresholdValue = alert.ThresholdValue.Trim();
        }

        if (alert.CreatedAtUtc == default)
        {
            alert.CreatedAtUtc = DateTime.UtcNow;
        }

        alert.IsActive = true;
    }

    private static string CreateCandidateKey(RiskAlertCandidate candidate)
    {
        return CreateAlertKey(
            candidate.Type,
            candidate.Symbol,
            candidate.SourceEndpoint);
    }

    private static string CreateAlertKey(RiskAlert alert)
    {
        return CreateAlertKey(
            alert.Type,
            alert.Symbol,
            alert.SourceEndpoint);
    }

    private static string CreateAlertKey(
        AlertType alertType,
        string? symbol,
        string sourceEndpoint)
    {
        var normalizedSymbol = NormalizeNullableSymbol(symbol) ?? "PORTFOLIO_OR_SYSTEM";
        var normalizedSourceEndpoint = sourceEndpoint.Trim().ToUpperInvariant();

        return $"{alertType}|{normalizedSymbol}|{normalizedSourceEndpoint}";
    }

    private static string? NormalizeNullableSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        return NormalizeSymbol(symbol);
    }

    private static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().ToUpperInvariant();
    }
}