using System.Text.Json;
using MegaFintradeRiskMonitor.Controllers;
using MegaFintradeRiskMonitor.Models;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Controllers;

public class AlertsControllerTests
{
    [Fact]
    public async Task GetAllAlerts_ReturnsOkWithAllAlerts()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.AddRange(
            CreateAlert(
                id: 1,
                symbol: null,
                type: AlertType.JavaBackendUnavailable,
                severity: AlertSeverity.Critical,
                isActive: true),
            CreateAlert(
                id: 2,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: false));

        var controller = new AlertsController(alertService);

        var result = await controller.GetAllAlerts(
            symbol: null,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var responseText = Serialize(okResult.Value);

        Assert.Contains("\"count\":2", responseText);
        Assert.Contains("JavaBackendUnavailable", responseText);
        Assert.Contains("LowSharpeRatio", responseText);
    }

    [Fact]
    public async Task GetAllAlerts_AppliesSymbolFilter()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.AddRange(
            CreateAlert(
                id: 1,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: true),
            CreateAlert(
                id: 2,
                symbol: "MSFT",
                type: AlertType.DrawdownBreach,
                severity: AlertSeverity.High,
                isActive: true));

        var controller = new AlertsController(alertService);

        var result = await controller.GetAllAlerts(
            symbol: "aapl",
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var responseText = Serialize(okResult.Value);

        Assert.Contains("AAPL", responseText);
        Assert.DoesNotContain("MSFT", responseText);
    }

    [Fact]
    public async Task GetActiveAlerts_ReturnsOnlyActiveAlerts()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.AddRange(
            CreateAlert(
                id: 1,
                symbol: null,
                type: AlertType.JavaBackendUnavailable,
                severity: AlertSeverity.Critical,
                isActive: true),
            CreateAlert(
                id: 2,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: false));

        var controller = new AlertsController(alertService);

        var result = await controller.GetActiveAlerts(
            symbol: null,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var responseText = Serialize(okResult.Value);

        Assert.Contains("\"count\":1", responseText);
        Assert.Contains("JavaBackendUnavailable", responseText);
        Assert.DoesNotContain("LowSharpeRatio", responseText);
    }

    [Fact]
    public async Task GetAlertHistory_ReturnsResolvedAlerts()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.AddRange(
            CreateAlert(
                id: 1,
                symbol: null,
                type: AlertType.JavaBackendUnavailable,
                severity: AlertSeverity.Critical,
                isActive: true),
            CreateAlert(
                id: 2,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: false,
                resolvedAtUtc: new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)));

        var controller = new AlertsController(alertService);

        var result = await controller.GetAlertHistory(
            symbol: null,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var responseText = Serialize(okResult.Value);

        Assert.Contains("\"count\":1", responseText);
        Assert.Contains("LowSharpeRatio", responseText);
        Assert.DoesNotContain("JavaBackendUnavailable", responseText);
    }

    [Fact]
    public async Task GetAlertById_ReturnsOk_WhenAlertExists()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.Add(
            CreateAlert(
                id: 10,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: true));

        var controller = new AlertsController(alertService);

        var result = await controller.GetAlertById(
            id: 10,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var alert = Assert.IsType<RiskAlert>(okResult.Value);

        Assert.Equal(10, alert.Id);
        Assert.Equal("AAPL", alert.Symbol);
        Assert.Equal(AlertType.LowSharpeRatio, alert.Type);
    }

    [Fact]
    public async Task GetAlertById_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        var alertService = new FakeAlertService();

        var controller = new AlertsController(alertService);

        var result = await controller.GetAlertById(
            id: 999,
            cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ResolveAlert_ReturnsOkAndMarksAlertResolved_WhenAlertExists()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.Add(
            CreateAlert(
                id: 20,
                symbol: "MSFT",
                type: AlertType.DrawdownBreach,
                severity: AlertSeverity.High,
                isActive: true));

        var controller = new AlertsController(alertService);

        var result = await controller.ResolveAlert(
            id: 20,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        var alert = alertService.Alerts.Single(existingAlert => existingAlert.Id == 20);

        Assert.False(alert.IsActive);
        Assert.NotNull(alert.ResolvedAtUtc);
    }

    [Fact]
    public async Task ResolveAlert_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        var alertService = new FakeAlertService();

        var controller = new AlertsController(alertService);

        var result = await controller.ResolveAlert(
            id: 999,
            cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAlert_ReturnsOkAndRemovesAlert_WhenAlertExists()
    {
        var alertService = new FakeAlertService();

        alertService.Alerts.Add(
            CreateAlert(
                id: 30,
                symbol: "AAPL",
                type: AlertType.LowSharpeRatio,
                severity: AlertSeverity.Medium,
                isActive: true));

        var controller = new AlertsController(alertService);

        var result = await controller.DeleteAlert(
            id: 30,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);
        Assert.Empty(alertService.Alerts);
    }

    [Fact]
    public async Task DeleteAlert_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        var alertService = new FakeAlertService();

        var controller = new AlertsController(alertService);

        var result = await controller.DeleteAlert(
            id: 999,
            cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static RiskAlert CreateAlert(
        long id,
        string? symbol,
        AlertType type,
        AlertSeverity severity,
        bool isActive,
        DateTime? resolvedAtUtc = null)
    {
        return new RiskAlert
        {
            Id = id,
            Symbol = symbol,
            Type = type,
            Severity = severity,
            Message = $"Test alert for {type}",
            SourceEndpoint = type == AlertType.JavaBackendUnavailable
                ? "JavaBackendApi"
                : "/api/reports/summary",
            SourceValue = "test-source-value",
            ThresholdValue = "test-threshold-value",
            IsActive = isActive,
            CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            ResolvedAtUtc = resolvedAtUtc
        };
    }

    private static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value);
    }
}