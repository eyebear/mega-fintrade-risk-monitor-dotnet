using Xunit;
using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace MegaFintradeRiskMonitor.Tests.Clients;

public class JavaBackendApiClientTests
{
    [Fact]
    public async Task IsBackendReachableAsync_ReturnsTrue_WhenReportSummaryEndpointReturnsSuccess()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddJsonResponse(
            "/api/reports/summary",
            """
            {
              "portfolioSharpeRatio": 1.25,
              "portfolioMaxDrawdown": -0.12,
              "latestEquityDate": "2026-05-07",
              "riskMetricRowCount": 4,
              "backtestResultRowCount": 12,
              "strategySignalRowCount": 16,
              "equityCurveRowCount": 100,
              "symbols": []
            }
            """);

        var client = CreateClient(handler);

        var result = await client.IsBackendReachableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsBackendReachableAsync_ReturnsFalse_WhenReportSummaryEndpointReturnsServerError()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddFailure(
            "/api/reports/summary",
            System.Net.HttpStatusCode.InternalServerError);

        var client = CreateClient(handler);

        var result = await client.IsBackendReachableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task GetReportSummaryAsync_ParsesPortfolioLevelSummary()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddJsonResponse(
            "/api/reports/summary",
            """
            {
              "portfolioSharpeRatio": 1.25,
              "portfolioMaxDrawdown": -0.12,
              "latestEquityDate": "2026-05-07",
              "riskMetricRowCount": 4,
              "backtestResultRowCount": 12,
              "strategySignalRowCount": 16,
              "equityCurveRowCount": 100,
              "symbols": []
            }
            """);

        var client = CreateClient(handler);

        var summary = await client.GetReportSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(1.25m, summary.PortfolioSharpeRatio);
        Assert.Equal(-0.12m, summary.PortfolioMaxDrawdown);
        Assert.Equal(new DateOnly(2026, 5, 7), summary.LatestEquityDate);
        Assert.Equal(4, summary.RiskMetricRowCount);
        Assert.Equal(12, summary.BacktestResultRowCount);
        Assert.Equal(16, summary.StrategySignalRowCount);
        Assert.Equal(100, summary.EquityCurveRowCount);
        Assert.Empty(summary.Symbols);
    }

    [Fact]
    public async Task GetReportSummaryAsync_ParsesDynamicSymbolMetrics()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddJsonResponse(
            "/api/reports/summary",
            """
            {
              "portfolioSharpeRatio": 1.10,
              "portfolioMaxDrawdown": -0.09,
              "latestEquityDate": "2026-05-07",
              "riskMetricRowCount": 4,
              "backtestResultRowCount": 12,
              "strategySignalRowCount": 16,
              "equityCurveRowCount": 100,
              "symbols": [
                {
                  "symbol": "AAPL",
                  "sharpeRatio": 1.25,
                  "maxDrawdown": -0.12,
                  "latestDataDate": "2026-05-07"
                },
                {
                  "symbol": "MSFT",
                  "sharpeRatio": 0.82,
                  "maxDrawdown": -0.18,
                  "latestDataDate": "2026-05-07"
                }
              ]
            }
            """);

        var client = CreateClient(handler);

        var summary = await client.GetReportSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(2, summary.Symbols.Count);

        var firstSymbol = summary.Symbols[0];
        Assert.Equal("AAPL", firstSymbol.Symbol);
        Assert.Equal(1.25m, firstSymbol.SharpeRatio);
        Assert.Equal(-0.12m, firstSymbol.MaxDrawdown);
        Assert.Equal(new DateOnly(2026, 5, 7), firstSymbol.LatestDataDate);

        var secondSymbol = summary.Symbols[1];
        Assert.Equal("MSFT", secondSymbol.Symbol);
        Assert.Equal(0.82m, secondSymbol.SharpeRatio);
        Assert.Equal(-0.18m, secondSymbol.MaxDrawdown);
        Assert.Equal(new DateOnly(2026, 5, 7), secondSymbol.LatestDataDate);
    }

    [Fact]
    public async Task GetImportAuditAsync_ParsesAuditRows()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddJsonResponse(
            "/api/import/audit",
            """
            [
              {
                "id": 10,
                "importType": "RISK_METRICS",
                "sourceFile": "risk_metrics.csv",
                "status": "COMPLETED",
                "totalRows": 4,
                "rejectedRows": 0,
                "startedAtUtc": "2026-05-07T17:00:00Z",
                "completedAtUtc": "2026-05-07T17:00:05Z"
              }
            ]
            """);

        var client = CreateClient(handler);

        var audits = await client.GetImportAuditAsync();

        Assert.Single(audits);

        var audit = audits[0];

        Assert.Equal(10, audit.Id);
        Assert.Equal("RISK_METRICS", audit.ImportType);
        Assert.Equal("risk_metrics.csv", audit.SourceFile);
        Assert.Equal("COMPLETED", audit.Status);
        Assert.Equal(4, audit.TotalRows);
        Assert.Equal(0, audit.RejectedRows);
        Assert.Equal(DateTime.Parse("2026-05-07T17:00:00Z").ToUniversalTime(), audit.StartedAtUtc);
        Assert.Equal(DateTime.Parse("2026-05-07T17:00:05Z").ToUniversalTime(), audit.CompletedAtUtc);
    }

    [Fact]
    public async Task GetImportRejectionsAsync_ParsesRejectedRows()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddJsonResponse(
            "/api/import/rejections",
            """
            [
              {
                "id": 3,
                "importType": "RISK_METRICS",
                "sourceFile": "risk_metrics.csv",
                "rowNumber": 7,
                "reason": "Invalid numeric value",
                "rawRow": "bad,row,data",
                "createdAtUtc": "2026-05-07T17:01:00Z"
              }
            ]
            """);

        var client = CreateClient(handler);

        var rejections = await client.GetImportRejectionsAsync();

        Assert.Single(rejections);

        var rejection = rejections[0];

        Assert.Equal(3, rejection.Id);
        Assert.Equal("RISK_METRICS", rejection.ImportType);
        Assert.Equal("risk_metrics.csv", rejection.SourceFile);
        Assert.Equal(7, rejection.RowNumber);
        Assert.Equal("Invalid numeric value", rejection.Reason);
        Assert.Equal(DateTime.Parse("2026-05-07T17:01:00Z").ToUniversalTime(), rejection.CreatedAtUtc);
    }

    [Fact]
    public async Task GetReportSummaryAsync_ReturnsNull_WhenBackendReturnsInvalidJson()
    {
        var handler = new FakeHttpMessageHandler();

        handler.AddTextResponse(
            "/api/reports/summary",
            "this is not json");

        var client = CreateClient(handler);

        var summary = await client.GetReportSummaryAsync();

        Assert.Null(summary);
    }

    private static JavaBackendApiClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        var httpClientFactory = new TestHttpClientFactory(httpClient);

        return new JavaBackendApiClient(
            httpClientFactory,
            NullLogger<JavaBackendApiClient>.Instance);
    }
}