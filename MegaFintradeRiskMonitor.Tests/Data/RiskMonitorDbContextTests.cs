using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Data;

public class RiskMonitorDbContextTests
{
    [Fact]
    public async Task Database_CanCreateRiskAlertTable()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var alert = new RiskAlert
        {
            Symbol = null,
            Type = AlertType.JavaBackendUnavailable,
            Severity = AlertSeverity.Critical,
            Message = "Java backend is unavailable.",
            SourceEndpoint = "JavaBackendApi",
            SourceValue = "reachable=false",
            ThresholdValue = "reachable=true",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        dbContext.RiskAlerts.Add(alert);

        await dbContext.SaveChangesAsync();

        var savedAlert = await dbContext.RiskAlerts.SingleAsync();

        Assert.True(savedAlert.Id > 0);
        Assert.Null(savedAlert.Symbol);
        Assert.Equal(AlertType.JavaBackendUnavailable, savedAlert.Type);
        Assert.Equal(AlertSeverity.Critical, savedAlert.Severity);
        Assert.True(savedAlert.IsActive);
    }

    [Fact]
    public async Task Database_CanCreateMonitoringSnapshotTable()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var snapshot = new MonitoringSnapshot
        {
            JavaBackendReachable = true,
            JavaBackendBaseUrl = "http://localhost:8080",
            ReportSummaryAvailable = true,
            ImportAuditAvailable = true,
            ImportRejectionsAvailable = false,
            PortfolioMonitoringAvailable = true,
            SymbolMonitoringAvailable = false,
            SymbolCount = 0,
            ActiveAlertCount = 1,
            CriticalAlertCount = 1,
            HighAlertCount = 0,
            MediumAlertCount = 0,
            LowAlertCount = 0,
            Status = "COMPLETED",
            Message = "Monitoring cycle completed.",
            CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
        };

        dbContext.MonitoringSnapshots.Add(snapshot);

        await dbContext.SaveChangesAsync();

        var savedSnapshot = await dbContext.MonitoringSnapshots.SingleAsync();

        Assert.True(savedSnapshot.Id > 0);
        Assert.True(savedSnapshot.JavaBackendReachable);
        Assert.Equal("http://localhost:8080", savedSnapshot.JavaBackendBaseUrl);
        Assert.Equal("COMPLETED", savedSnapshot.Status);
        Assert.Equal(1, savedSnapshot.ActiveAlertCount);
    }

    [Fact]
    public async Task MonitoringSnapshots_CanQueryLatestSnapshotByCreatedAtUtc()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        dbContext.MonitoringSnapshots.AddRange(
            CreateSnapshot(
                status: "OLDER",
                createdAtUtc: new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc)),
            CreateSnapshot(
                status: "LATEST",
                createdAtUtc: new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)));

        await dbContext.SaveChangesAsync();

        var latestSnapshot = await dbContext.MonitoringSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.CreatedAtUtc)
            .FirstAsync();

        Assert.Equal("LATEST", latestSnapshot.Status);
    }

    [Fact]
    public async Task AlertService_ResolveAlertAsync_MarksAlertInactiveAndSetsResolvedAtUtc()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var savedAlerts = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate>
            {
                CreateCandidate(
                    symbol: null,
                    type: AlertType.JavaBackendUnavailable,
                    severity: AlertSeverity.Critical,
                    sourceEndpoint: "JavaBackendApi")
            });

        var alertId = savedAlerts.Single().Id;

        var resolvedAlert = await service.ResolveAlertAsync(alertId);

        Assert.NotNull(resolvedAlert);
        Assert.False(resolvedAlert.IsActive);
        Assert.NotNull(resolvedAlert.ResolvedAtUtc);

        var alertFromDatabase = await dbContext.RiskAlerts
            .AsNoTracking()
            .SingleAsync(alert => alert.Id == alertId);

        Assert.False(alertFromDatabase.IsActive);
        Assert.NotNull(alertFromDatabase.ResolvedAtUtc);
    }

    [Fact]
    public async Task AlertService_GetAlertHistoryAsync_ReturnsResolvedAlerts()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var savedAlerts = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate>
            {
                CreateCandidate(
                    symbol: "AAPL",
                    type: AlertType.LowSharpeRatio,
                    severity: AlertSeverity.Medium,
                    sourceEndpoint: "/api/reports/summary")
            });

        var alertId = savedAlerts.Single().Id;

        await service.ResolveAlertAsync(alertId);

        var history = await service.GetAlertHistoryAsync();

        var historicalAlert = Assert.Single(history);

        Assert.Equal(alertId, historicalAlert.Id);
        Assert.False(historicalAlert.IsActive);
        Assert.NotNull(historicalAlert.ResolvedAtUtc);
    }

    [Fact]
    public async Task AlertService_DeleteAlertAsync_RemovesAlertFromDatabase()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var savedAlerts = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate>
            {
                CreateCandidate(
                    symbol: "AAPL",
                    type: AlertType.DrawdownBreach,
                    severity: AlertSeverity.High,
                    sourceEndpoint: "/api/reports/summary")
            });

        var alertId = savedAlerts.Single().Id;

        var deleted = await service.DeleteAlertAsync(alertId);

        Assert.True(deleted);
        Assert.Equal(0, await dbContext.RiskAlerts.CountAsync());
    }

    [Fact]
    public async Task AlertService_DeleteAlertAsync_ReturnsFalse_WhenAlertDoesNotExist()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var deleted = await service.DeleteAlertAsync(999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task AlertService_GetAlertByIdAsync_ReturnsExpectedAlert()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var savedAlerts = await service.SaveAlertCandidatesAsync(
            new List<RiskAlertCandidate>
            {
                CreateCandidate(
                    symbol: "MSFT",
                    type: AlertType.LowSharpeRatio,
                    severity: AlertSeverity.Medium,
                    sourceEndpoint: "/api/reports/summary")
            });

        var savedAlert = savedAlerts.Single();

        var alertById = await service.GetAlertByIdAsync(savedAlert.Id);

        Assert.NotNull(alertById);
        Assert.Equal(savedAlert.Id, alertById.Id);
        Assert.Equal("MSFT", alertById.Symbol);
        Assert.Equal(AlertType.LowSharpeRatio, alertById.Type);
    }

    [Fact]
    public async Task AlertService_GetAllAlertsAsync_ReturnsActiveAndResolvedAlerts()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var service = CreateAlertService(dbContext);

        var savedAlerts = await service.SaveAlertCandidatesAsync(
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

        await service.ResolveAlertAsync(savedAlerts[0].Id);

        var allAlerts = await service.GetAllAlertsAsync();

        Assert.Equal(2, allAlerts.Count);
        Assert.Contains(allAlerts, alert => !alert.IsActive);
        Assert.Contains(allAlerts, alert => alert.IsActive);
    }

    [Fact]
    public async Task Database_ModelContainsDuplicatePreventionIndex()
    {
        await using var dbContext = await TestDbContextFactory.CreateSqliteContextAsync();

        var entityType = dbContext.Model.FindEntityType(typeof(RiskAlert));

        Assert.NotNull(entityType);

        var hasDuplicatePreventionIndex = entityType
            .GetIndexes()
            .Any(index =>
            {
                var propertyNames = index.Properties
                    .Select(property => property.Name)
                    .ToList();

                return propertyNames.SequenceEqual(new[]
                {
                    nameof(RiskAlert.Type),
                    nameof(RiskAlert.Symbol),
                    nameof(RiskAlert.SourceEndpoint),
                    nameof(RiskAlert.IsActive)
                });
            });

        Assert.True(hasDuplicatePreventionIndex);
    }

    private static AlertService CreateAlertService(RiskMonitorDbContext dbContext)
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

    private static MonitoringSnapshot CreateSnapshot(
        string status,
        DateTime createdAtUtc)
    {
        return new MonitoringSnapshot
        {
            JavaBackendReachable = true,
            JavaBackendBaseUrl = "http://localhost:8080",
            ReportSummaryAvailable = true,
            ImportAuditAvailable = true,
            ImportRejectionsAvailable = false,
            PortfolioMonitoringAvailable = true,
            SymbolMonitoringAvailable = false,
            SymbolCount = 0,
            ActiveAlertCount = 0,
            CriticalAlertCount = 0,
            HighAlertCount = 0,
            MediumAlertCount = 0,
            LowAlertCount = 0,
            Status = status,
            Message = $"Snapshot status {status}",
            CreatedAtUtc = createdAtUtc
        };
    }
}