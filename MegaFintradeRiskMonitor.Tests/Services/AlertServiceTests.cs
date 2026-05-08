using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class AlertServiceTests
{
    [Fact]
    public async Task SaveAlertCandidatesAsync_SavesNewAlert_WhenNoDuplicateExists()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var candidates = new List<RiskAlertCandidate>
        {
            CreateCandidate(
                symbol: null,
                type: AlertType.JavaBackendUnavailable,
                severity: AlertSeverity.Critical,
                sourceEndpoint: "JavaBackendApi")
        };

        var savedAlerts = await service.SaveAlertCandidatesAsync(candidates);

        Assert.Single(savedAlerts);
        Assert.Equal(1, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_SkipsDuplicateActiveAlert_WhenTypeSymbolAndEndpointMatch()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.JavaBackendUnavailable,
            severity: AlertSeverity.Critical,
            sourceEndpoint: "JavaBackendApi");

        var secondCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.JavaBackendUnavailable,
            severity: AlertSeverity.Critical,
            sourceEndpoint: "JavaBackendApi");

        var firstSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(firstSave);
        Assert.Empty(secondSave);
        Assert.Equal(1, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_SavesAlert_WhenSameTypeAndEndpointButDifferentSymbol()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var secondCandidate = CreateCandidate(
            symbol: "MSFT",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(secondSave);
        Assert.Equal(2, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_SavesAlert_WhenSameSymbolAndEndpointButDifferentType()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var secondCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.DrawdownBreach,
            severity: AlertSeverity.High,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(secondSave);
        Assert.Equal(2, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_SavesAlert_WhenSameTypeAndSymbolButDifferentEndpoint()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var secondCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/other");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(secondSave);
        Assert.Equal(2, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_SavesNewAlert_WhenPreviousDuplicateWasResolved()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.JavaBackendUnavailable,
            severity: AlertSeverity.Critical,
            sourceEndpoint: "JavaBackendApi");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var existingAlert = dbContext.RiskAlerts.Single();

        existingAlert.IsActive = false;
        existingAlert.ResolvedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var secondCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.JavaBackendUnavailable,
            severity: AlertSeverity.Critical,
            sourceEndpoint: "JavaBackendApi");

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(secondSave);
        Assert.Equal(2, dbContext.RiskAlerts.Count());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_NormalizesSymbolBeforeDuplicateCheck()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "aapl",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var secondCandidate = CreateCandidate(
            symbol: " AAPL ",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Empty(secondSave);
        Assert.Equal(1, dbContext.RiskAlerts.Count());

        var storedAlert = dbContext.RiskAlerts.Single();

        Assert.Equal("AAPL", storedAlert.Symbol);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsOnlyActiveAlerts()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        var activeCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.JavaBackendUnavailable,
            severity: AlertSeverity.Critical,
            sourceEndpoint: "JavaBackendApi");

        var resolvedCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { activeCandidate, resolvedCandidate });

        var alertToResolve = dbContext.RiskAlerts.Single(alert => alert.Symbol == "AAPL");

        alertToResolve.IsActive = false;
        alertToResolve.ResolvedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var activeAlerts = await service.GetActiveAlertsAsync();

        Assert.Single(activeAlerts);
        Assert.Equal(AlertType.JavaBackendUnavailable, activeAlerts[0].Type);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_AppliesSymbolFilter()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateService(dbContext);

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate>
            {
                CreateCandidate(
                    symbol: "AAPL",
                    type: AlertType.LowSharpeRatio,
                    severity: AlertSeverity.Medium,
                    sourceEndpoint: "/api/reports/summary"),
                CreateCandidate(
                    symbol: "MSFT",
                    type: AlertType.DrawdownBreach,
                    severity: AlertSeverity.High,
                    sourceEndpoint: "/api/reports/summary")
            });

        var activeAlerts = await service.GetActiveAlertsAsync("aapl");

        Assert.Single(activeAlerts);
        Assert.Equal("AAPL", activeAlerts[0].Symbol);
    }

    private static AlertService CreateService(MegaFintradeRiskMonitor.Data.RiskMonitorDbContext dbContext)
    {
        return new AlertService(
            dbContext,
            NullLogger<AlertService>.Instance);
    }

    private static RiskAlertCandidate CreateCandidate(
        string? symbol,
        AlertType type,
        AlertSeverity severity,
        string sourceEndpoint)
    {
        return new RiskAlertCandidate
        {
            Symbol = symbol,
            Type = type,
            Severity = severity,
            Message = $"Test alert for {type}",
            SourceEndpoint = sourceEndpoint,
            SourceValue = "test-source-value",
            ThresholdValue = "test-threshold-value"
        };
    }
}