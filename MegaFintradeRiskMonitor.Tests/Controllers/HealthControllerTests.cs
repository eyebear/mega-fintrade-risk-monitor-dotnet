using MegaFintradeRiskMonitor.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void GetHealthShouldReturnUpStatusAndServiceName()
    {
        var controller = new HealthController();

        var result = controller.GetHealth();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);

        Assert.Equal("UP", response.Status);
        Assert.Equal("mega-fintrade-risk-monitor-dotnet", response.Service);
        Assert.True(response.TimestampUtc <= DateTime.UtcNow);
        Assert.True(response.TimestampUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}