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
            role = "Project 4 - C#/.NET Risk Monitoring and Alerting Service",
            timestampUtc = DateTime.UtcNow
        });
    }
}