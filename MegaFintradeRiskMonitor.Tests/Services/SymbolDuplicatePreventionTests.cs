using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class SymbolDuplicatePreventionTests
{
    [Fact]
    public async Task SaveAlertCandidatesAsync_SkipsDuplicate_WhenTypeSymbolEndpointAndActiveStatusMatch()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var duplicateCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var firstSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { duplicateCandidate });

        Assert.Single(firstSave);
        Assert.Empty(secondSave);
        Assert.Equal(1, await dbContext.RiskAlerts.CountAsync());
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_DoesNotBlockDifferentSymbols()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var aaplCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var msftCandidate = CreateCandidate(
            symbol: "MSFT",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { aaplCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { msftCandidate });

        Assert.Single(secondSave);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Symbol)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.Equal("AAPL", alerts[0].Symbol);
        Assert.Equal("MSFT", alerts[1].Symbol);
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_DoesNotBlockPortfolioAlertWhenSymbolAlertExists()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var symbolCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var portfolioCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { symbolCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { portfolioCandidate });

        Assert.Single(secondSave);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Symbol == null ? "" : alert.Symbol)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, alert => alert.Symbol == "AAPL");
        Assert.Contains(alerts, alert => alert.Symbol is null);
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_DoesNotBlockSymbolAlertWhenPortfolioAlertExists()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var portfolioCandidate = CreateCandidate(
            symbol: null,
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var symbolCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { portfolioCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { symbolCandidate });

        Assert.Single(secondSave);

        var alerts = await dbContext.RiskAlerts.ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, alert => alert.Symbol is null);
        Assert.Contains(alerts, alert => alert.Symbol == "AAPL");
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_DoesNotBlockDifferentAlertTypesForSameSymbol()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var lowSharpeCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var drawdownCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.DrawdownBreach,
            severity: AlertSeverity.High,
            sourceEndpoint: "/api/reports/summary");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { lowSharpeCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { drawdownCandidate });

        Assert.Single(secondSave);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.Type)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, alert => Assert.Equal("AAPL", alert.Symbol));

        Assert.Contains(alerts, alert => alert.Type == AlertType.LowSharpeRatio);
        Assert.Contains(alerts, alert => alert.Type == AlertType.DrawdownBreach);
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_DoesNotBlockDifferentEndpointsForSameSymbolAndType()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var reportEndpointCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var otherEndpointCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/other");

        await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { reportEndpointCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { otherEndpointCandidate });

        Assert.Single(secondSave);

        var alerts = await dbContext.RiskAlerts
            .OrderBy(alert => alert.SourceEndpoint)
            .ToListAsync();

        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, alert => Assert.Equal("AAPL", alert.Symbol));
        Assert.Contains(alerts, alert => alert.SourceEndpoint == "/api/reports/summary");
        Assert.Contains(alerts, alert => alert.SourceEndpoint == "/api/other");
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_NormalizesSymbolBeforeCheckingDuplicates()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

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

        var firstSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(firstSave);
        Assert.Empty(secondSave);

        var alert = await dbContext.RiskAlerts.SingleAsync();

        Assert.Equal("AAPL", alert.Symbol);
    }

    [Fact]
    public async Task SaveAlertCandidatesAsync_AllowsNewAlertAfterSameSymbolDuplicateIsResolved()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var firstCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var firstSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { firstCandidate });

        var firstAlert = firstSave.Single();

        await service.ResolveAlertAsync(firstAlert.Id);

        var secondCandidate = CreateCandidate(
            symbol: "AAPL",
            type: AlertType.LowSharpeRatio,
            severity: AlertSeverity.Medium,
            sourceEndpoint: "/api/reports/summary");

        var secondSave = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate> { secondCandidate });

        Assert.Single(secondSave);
        Assert.Equal(2, await dbContext.RiskAlerts.CountAsync());

        var activeAlerts = await service.GetActiveAlertsAsync("AAPL");

        Assert.Single(activeAlerts);
        Assert.True(activeAlerts[0].IsActive);
        Assert.Equal("AAPL", activeAlerts[0].Symbol);
    }

    private static AlertService CreateAlertService(
        global::MegaFintradeRiskMonitor.Data.RiskMonitorDbContext dbContext)
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
            Message = $"Test alert for {symbol ?? "portfolio"} {type}",
            SourceEndpoint = sourceEndpoint,
            SourceValue = "test-source-value",
            ThresholdValue = "test-threshold-value"
        };
    }
}