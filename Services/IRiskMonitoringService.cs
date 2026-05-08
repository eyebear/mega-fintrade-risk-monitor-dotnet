namespace MegaFintradeRiskMonitor.Services;

public interface IRiskMonitoringService
{
    Task<RiskMonitoringRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default);
}