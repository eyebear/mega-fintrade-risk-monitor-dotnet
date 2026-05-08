using MegaFintradeRiskMonitor.Options;
using Microsoft.Extensions.Options;

namespace MegaFintradeRiskMonitor.Services;

public class RiskMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<MonitoringOptions> _monitoringOptions;
    private readonly ILogger<RiskMonitoringBackgroundService> _logger;

    public RiskMonitoringBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<MonitoringOptions> monitoringOptions,
        ILogger<RiskMonitoringBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _monitoringOptions = monitoringOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Risk monitoring background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMonitoringCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Risk monitoring background service cancellation requested.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error occurred during risk monitoring background cycle.");
            }

            var delaySeconds = Math.Max(
                10,
                _monitoringOptions.CurrentValue.PollingIntervalSeconds);

            _logger.LogInformation(
                "Risk monitoring background service waiting {DelaySeconds} seconds before next cycle.",
                delaySeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(delaySeconds),
                stoppingToken);
        }

        _logger.LogInformation(
            "Risk monitoring background service stopped.");
    }

    private async Task RunMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var riskMonitoringService = scope.ServiceProvider
            .GetRequiredService<IRiskMonitoringService>();

        var result = await riskMonitoringService.RunOnceAsync(cancellationToken);

        _logger.LogInformation(
            "Background monitoring cycle completed. Status={Status}, SnapshotId={SnapshotId}, AlertCandidateCount={AlertCandidateCount}, SavedAlertCount={SavedAlertCount}.",
            result.Status,
            result.MonitoringSnapshotId,
            result.AlertCandidateCount,
            result.SavedAlertCount);
    }
}