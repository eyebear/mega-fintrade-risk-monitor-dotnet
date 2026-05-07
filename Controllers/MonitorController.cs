using MegaFintradeRiskMonitor.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Controllers;

[ApiController]
[Route("api/monitor")]
public class MonitorController : ControllerBase
{
    private readonly Project1ApiOptions _project1ApiOptions;
    private readonly AiIntegrationOptions _aiIntegrationOptions;
    private readonly AlertRuleOptions _alertRuleOptions;
    private readonly MonitoringOptions _monitoringOptions;

    public MonitorController(
        IOptions<Project1ApiOptions> project1ApiOptions,
        IOptions<AiIntegrationOptions> aiIntegrationOptions,
        IOptions<AlertRuleOptions> alertRuleOptions,
        IOptions<MonitoringOptions> monitoringOptions)
    {
        _project1ApiOptions = project1ApiOptions.Value;
        _aiIntegrationOptions = aiIntegrationOptions.Value;
        _alertRuleOptions = alertRuleOptions.Value;
        _monitoringOptions = monitoringOptions.Value;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            service = "mega-fintrade-risk-monitor-dotnet",
            status = "CONFIGURED",
            project1 = new
            {
                baseUrl = _project1ApiOptions.BaseUrl,
                timeoutSeconds = _project1ApiOptions.TimeoutSeconds,
                plannedEndpoints = new[]
                {
                    "/api/reports/summary",
                    "/api/import/audit",
                    "/api/import/rejections"
                }
            },
            alertRules = new
            {
                maxDrawdownThreshold = _alertRuleOptions.MaxDrawdownThreshold,
                minimumSharpeRatio = _alertRuleOptions.MinimumSharpeRatio,
                staleDataDays = _alertRuleOptions.StaleDataDays,
                csvRejectionThreshold = _alertRuleOptions.CsvRejectionThreshold
            },
            monitoring = new
            {
                pollingIntervalSeconds = _monitoringOptions.PollingIntervalSeconds
            },
            aiIntegration = new
            {
                enabled = _aiIntegrationOptions.Enabled,
                project5BaseUrl = _aiIntegrationOptions.Project5BaseUrl,
                mode = _aiIntegrationOptions.Enabled ? "OPTIONAL_EXTERNAL_SERVICE" : "DISABLED"
            },
            timestampUtc = DateTime.UtcNow
        });
    }
}