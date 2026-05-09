using Microsoft.AspNetCore.Mvc;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "UP",
            Service = "mega-fintrade-risk-monitor-dotnet",
            TimestampUtc = DateTime.UtcNow
        });
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;

    public string Service { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}