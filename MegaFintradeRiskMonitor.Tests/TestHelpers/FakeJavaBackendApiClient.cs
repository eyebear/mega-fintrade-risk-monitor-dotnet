using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Dtos.Project1;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public class FakeJavaBackendApiClient : IJavaBackendApiClient
{
    public bool BackendReachable { get; set; } = true;

    public JavaBackendReportSummaryDto? ReportSummary { get; set; }

    public IReadOnlyList<JavaBackendImportAuditDto> ImportAudits { get; set; } =
        Array.Empty<JavaBackendImportAuditDto>();

    public IReadOnlyList<JavaBackendImportRejectionDto> ImportRejections { get; set; } =
        Array.Empty<JavaBackendImportRejectionDto>();

    public int ReachabilityCallCount { get; private set; }

    public int ReportSummaryCallCount { get; private set; }

    public int ImportAuditCallCount { get; private set; }

    public int ImportRejectionsCallCount { get; private set; }

    public Task<bool> IsBackendReachableAsync(
        CancellationToken cancellationToken = default)
    {
        ReachabilityCallCount++;

        return Task.FromResult(BackendReachable);
    }

    public Task<JavaBackendReportSummaryDto?> GetReportSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        ReportSummaryCallCount++;

        return Task.FromResult(ReportSummary);
    }

    public Task<IReadOnlyList<JavaBackendImportAuditDto>> GetImportAuditAsync(
        CancellationToken cancellationToken = default)
    {
        ImportAuditCallCount++;

        return Task.FromResult(ImportAudits);
    }

    public Task<IReadOnlyList<JavaBackendImportRejectionDto>> GetImportRejectionsAsync(
        CancellationToken cancellationToken = default)
    {
        ImportRejectionsCallCount++;

        return Task.FromResult(ImportRejections);
    }
}