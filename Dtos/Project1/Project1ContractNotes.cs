namespace MegaFintradeRiskMonitor.Dtos.Project1;

/// <summary>
/// Contract notes for Project 1 integration.
///
/// Current confirmed Project 1 public README endpoints:
/// - GET /api/positions
/// - POST /api/positions
/// - POST /api/positions/batch
/// - GET /api/positions/{id}
/// - GET /api/positions/{id}/pnl/{price}
///
/// Planned Project 4 monitoring endpoints from Project 1:
/// - GET /api/reports/summary
/// - GET /api/import/audit
/// - GET /api/import/rejections
///
/// Do not finalize monitoring DTO fields until Project 1 exposes and documents
/// the final JSON response contracts for the reporting and import endpoints.
/// </summary>
public static class Project1ContractNotes
{
    public const string PositionsEndpoint = "/api/positions";

    public const string PlannedReportSummaryEndpoint = "/api/reports/summary";

    public const string PlannedImportAuditEndpoint = "/api/import/audit";

    public const string PlannedImportRejectionsEndpoint = "/api/import/rejections";
}