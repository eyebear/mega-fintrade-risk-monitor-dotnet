using MegaFintradeRiskMonitor.Dtos.Ai;
using MegaFintradeRiskMonitor.Options;
using MegaFintradeRiskMonitor.Services;
using Xunit;

namespace MegaFintradeRiskMonitor.Tests.Services;

public class DisabledAiDecisionSupportClientTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsDisabledStatus_WhenAiIntegrationDisabled()
    {
        var client = CreateClient(enabled: false);

        var status = await client.GetStatusAsync();

        Assert.False(status.Enabled);
        Assert.False(status.AdvisorReachable);
        Assert.Equal("DISABLED", status.Mode);
        Assert.Equal("AI_DISABLED", status.Status);
        Assert.Equal("http://localhost:7005", status.Project5BaseUrl);
        Assert.True(status.ProviderSelectionOwnedByAdvisor);
        Assert.False(status.ApiTokensStoredInRiskMonitor);
        Assert.Contains("disabled", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsPlaceholderStatus_WhenAiIntegrationEnabledButNoAdvisorClientImplemented()
    {
        var client = CreateClient(enabled: true);

        var status = await client.GetStatusAsync();

        Assert.True(status.Enabled);
        Assert.False(status.AdvisorReachable);
        Assert.Equal("PLACEHOLDER_CLIENT_CONFIGURED", status.Mode);
        Assert.Equal("AI_ADVISOR_NOT_CONNECTED", status.Status);
        Assert.Equal("http://localhost:7005", status.Project5BaseUrl);
        Assert.True(status.ProviderSelectionOwnedByAdvisor);
        Assert.False(status.ApiTokensStoredInRiskMonitor);
        Assert.Contains("no AI advisor client", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRiskBriefAsync_ReturnsUnavailableBrief_WhenAiIntegrationDisabled()
    {
        var client = CreateClient(enabled: false);

        var request = new AiRiskBriefRequestDto
        {
            RequestedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            Portfolio = new AiPortfolioRiskSnapshotDto
            {
                SharpeRatio = 1.25m,
                MaxDrawdown = -0.10m,
                LatestEquityDate = new DateOnly(2026, 5, 7)
            },
            Symbols = new List<AiSymbolRiskSnapshotDto>
            {
                new()
                {
                    Symbol = "AAPL",
                    SharpeRatio = 1.20m,
                    MaxDrawdown = -0.12m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                }
            },
            ActiveAlerts = Array.Empty<AiAlertSnapshotDto>()
        };

        var result = await client.GetRiskBriefAsync(request);

        Assert.False(result.Available);
        Assert.Equal("AI_DISABLED", result.Status);
        Assert.Contains("not available", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deterministic monitor", result.RecommendedAction, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.KeyRisks);
        Assert.Empty(result.SymbolHighlights);
    }

    [Fact]
    public async Task GetRiskBriefAsync_ReturnsUnavailableBrief_WhenAiIntegrationEnabledButAdvisorNotConnected()
    {
        var client = CreateClient(enabled: true);

        var request = new AiRiskBriefRequestDto
        {
            RequestedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
            Portfolio = new AiPortfolioRiskSnapshotDto
            {
                SharpeRatio = 0.80m,
                MaxDrawdown = -0.25m,
                LatestEquityDate = new DateOnly(2026, 5, 7)
            },
            Symbols = new List<AiSymbolRiskSnapshotDto>
            {
                new()
                {
                    Symbol = "MSFT",
                    SharpeRatio = 0.75m,
                    MaxDrawdown = -0.25m,
                    LatestDataDate = new DateOnly(2026, 5, 7)
                }
            },
            ActiveAlerts = new List<AiAlertSnapshotDto>
            {
                new()
                {
                    Symbol = "MSFT",
                    Type = "LowSharpeRatio",
                    Severity = "Medium",
                    Message = "Symbol MSFT Sharpe ratio is below threshold.",
                    SourceEndpoint = "/api/reports/summary",
                    CreatedAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        var result = await client.GetRiskBriefAsync(request);

        Assert.False(result.Available);
        Assert.Equal("AI_ADVISOR_NOT_CONNECTED", result.Status);
        Assert.Contains("not available", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.KeyRisks);
        Assert.Empty(result.SymbolHighlights);
    }

    [Fact]
    public async Task GetStatusAsync_DoesNotExposeAnyProviderSecretFields()
    {
        var client = CreateClient(enabled: true);

        var status = await client.GetStatusAsync();

        var propertyNames = status.GetType()
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        Assert.DoesNotContain(propertyNames, propertyName =>
            propertyName.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(propertyNames, propertyName =>
            propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(propertyNames, propertyName =>
            propertyName.Equals("GeminiApiKey", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(propertyNames, propertyName =>
            propertyName.Equals("GrokApiKey", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(propertyNames, propertyName =>
            propertyName.Equals("OpenAiApiKey", StringComparison.OrdinalIgnoreCase));

        Assert.False(status.ApiTokensStoredInRiskMonitor);
    }

    private static DisabledAiDecisionSupportClient CreateClient(bool enabled)
    {
        return new DisabledAiDecisionSupportClient(
            Microsoft.Extensions.Options.Options.Create(
                new AiIntegrationOptions
                {
                    Enabled = enabled,
                    Project5BaseUrl = "http://localhost:7005"
                }));
    }
}