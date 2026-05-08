using MegaFintradeRiskMonitor.Dtos.Project1;

namespace MegaFintradeRiskMonitor.Services;

public class AlertRuleEvaluationRequest
{
    public bool JavaBackendReachable { get; set; }

    public JavaBackendReportSummaryDto? ReportSummary { get; set; }

    public IReadOnlyList<JavaBackendImportAuditDto> ImportAudits { get; set; } =
        Array.Empty<JavaBackendImportAuditDto>();

    public IReadOnlyList<JavaBackendImportRejectionDto> ImportRejections { get; set; } =
        Array.Empty<JavaBackendImportRejectionDto>();

    public DateTime EvaluationTimeUtc { get; set; } = DateTime.UtcNow;
}