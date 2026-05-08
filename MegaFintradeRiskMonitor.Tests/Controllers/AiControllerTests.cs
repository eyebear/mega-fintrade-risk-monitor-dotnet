using MegaFintradeRiskMonitor.Controllers;
using MegaFintradeRiskMonitor.Dtos.Ai;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Controllers;

public class AiControllerTests
{
    [Fact]
    public async Task GetStatus_ReturnsOkWithAiDisabledStatus()
    {
        var aiClient = new FakeAiDecisionSupportClient
        {
            StatusToReturn = new AiIntegrationStatusDto
            {
                Enabled = false,
                Mode = "DISABLED",
                Project5BaseUrl = "http://localhost:7005",
                AdvisorReachable = false,
                Status = "AI_DISABLED",
                Message = "AI integration is disabled.",
                ProviderSelectionOwnedByAdvisor = true,
                ApiTokensStoredInRiskMonitor = false,
                CheckedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var controller = new AiController(aiClient);

        var result = await controller.GetStatus(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<AiIntegrationStatusDto>(okResult.Value);

        Assert.False(status.Enabled);
        Assert.False(status.AdvisorReachable);
        Assert.Equal("DISABLED", status.Mode);
        Assert.Equal("AI_DISABLED", status.Status);
        Assert.False(status.ApiTokensStoredInRiskMonitor);
        Assert.True(status.ProviderSelectionOwnedByAdvisor);
        Assert.Equal(1, aiClient.GetStatusCallCount);
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithPlaceholderConfiguredStatus()
    {
        var aiClient = new FakeAiDecisionSupportClient
        {
            StatusToReturn = new AiIntegrationStatusDto
            {
                Enabled = true,
                Mode = "PLACEHOLDER_CLIENT_CONFIGURED",
                Project5BaseUrl = "http://localhost:7005",
                AdvisorReachable = false,
                Status = "AI_ADVISOR_NOT_CONNECTED",
                Message = "AI advisor client is not implemented yet.",
                ProviderSelectionOwnedByAdvisor = true,
                ApiTokensStoredInRiskMonitor = false,
                CheckedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var controller = new AiController(aiClient);

        var result = await controller.GetStatus(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<AiIntegrationStatusDto>(okResult.Value);

        Assert.True(status.Enabled);
        Assert.False(status.AdvisorReachable);
        Assert.Equal("PLACEHOLDER_CLIENT_CONFIGURED", status.Mode);
        Assert.Equal("AI_ADVISOR_NOT_CONNECTED", status.Status);
        Assert.False(status.ApiTokensStoredInRiskMonitor);
        Assert.True(status.ProviderSelectionOwnedByAdvisor);
        Assert.Equal(1, aiClient.GetStatusCallCount);
    }
}