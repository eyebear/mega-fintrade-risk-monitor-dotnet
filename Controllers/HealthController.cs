using Microsoft.AspNetCore.Mvc;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            service = "mega-fintrade-risk-monitor-dotnet",
            status = "UP",
            role = "Mega Fintrade Risk Monitor .NET",
            description = "C#/.NET risk monitoring, alerting, dashboard, and AI-ready integration service",
            timestampUtc = DateTime.UtcNow
        });
    }
}