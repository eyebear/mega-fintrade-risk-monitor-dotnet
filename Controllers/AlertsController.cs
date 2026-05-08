using MegaFintradeRiskMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAlerts(
        [FromQuery] string? symbol,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetAllAlertsAsync(
            symbol,
            cancellationToken);

        return Ok(new
        {
            count = alerts.Count,
            symbolFilter = NormalizeSymbolForResponse(symbol),
            alerts
        });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAlerts(
        [FromQuery] string? symbol,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetActiveAlertsAsync(
            symbol,
            cancellationToken);

        return Ok(new
        {
            count = alerts.Count,
            symbolFilter = NormalizeSymbolForResponse(symbol),
            alerts
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetAlertHistory(
        [FromQuery] string? symbol,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetAlertHistoryAsync(
            symbol,
            cancellationToken);

        return Ok(new
        {
            count = alerts.Count,
            symbolFilter = NormalizeSymbolForResponse(symbol),
            alerts
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetAlertById(
        long id,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetAlertByIdAsync(
            id,
            cancellationToken);

        if (alert is null)
        {
            return NotFound(new
            {
                message = $"Alert with id {id} was not found."
            });
        }

        return Ok(alert);
    }

    [HttpPost("{id:long}/resolve")]
    public async Task<IActionResult> ResolveAlert(
        long id,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.ResolveAlertAsync(
            id,
            cancellationToken);

        if (alert is null)
        {
            return NotFound(new
            {
                message = $"Alert with id {id} was not found."
            });
        }

        return Ok(new
        {
            message = $"Alert with id {id} was resolved.",
            alert
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAlert(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _alertService.DeleteAlertAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = $"Alert with id {id} was not found."
            });
        }

        return Ok(new
        {
            message = $"Alert with id {id} was deleted."
        });
    }

    private static string? NormalizeSymbolForResponse(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        return symbol.Trim().ToUpperInvariant();
    }
}