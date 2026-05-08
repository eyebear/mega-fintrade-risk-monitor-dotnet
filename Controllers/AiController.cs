using MegaFintradeRiskMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiDecisionSupportClient _aiDecisionSupportClient;

    public AiController(IAiDecisionSupportClient aiDecisionSupportClient)
    {
        _aiDecisionSupportClient = aiDecisionSupportClient;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _aiDecisionSupportClient.GetStatusAsync(cancellationToken);

        return Ok(status);
    }
}