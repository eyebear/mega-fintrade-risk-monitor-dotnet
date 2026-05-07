using MegaFintradeRiskMonitor.Dtos.Project1;

namespace MegaFintradeRiskMonitor.Clients;

public interface IJavaBackendApiClient
{
    Task<bool> IsBackendReachableAsync(CancellationToken cancellationToken = default);

    Task<JavaBackendReportSummaryDto?> GetReportSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JavaBackendImportAuditDto>> GetImportAuditAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JavaBackendImportRejectionDto>> GetImportRejectionsAsync(
        CancellationToken cancellationToken = default);
}