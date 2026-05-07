namespace MegaFintradeRiskMonitor.Dtos.Project1;

/// <summary>
/// Contract notes for Mega Fintrade Backend Java integration.
///
/// Planned monitoring endpoints:
/// - GET /api/reports/summary
/// - GET /api/import/audit
/// - GET /api/import/rejections
///
/// Dynamic symbol support rule:
/// Mega Fintrade Risk Monitor must not hard-code stock symbols.
/// The Java backend may currently expose data derived from a fixed upstream
/// strategy symbol list, but this monitor should remain symbol-agnostic.
///
/// When the Java backend exposes symbol-level metrics, the monitor should
/// consume and display whatever symbols are returned by the backend.
///
/// If the Java backend only exposes portfolio-level metrics, the monitor should
/// continue operating with portfolio-level and system-level alerts only.
/// </summary>
public static class Project1ContractNotes
{
    public const string PlannedReportSummaryEndpoint = "/api/reports/summary";

    public const string PlannedImportAuditEndpoint = "/api/import/audit";

    public const string PlannedImportRejectionsEndpoint = "/api/import/rejections";
}