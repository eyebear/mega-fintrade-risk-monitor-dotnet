namespace MegaFintradeRiskMonitor.Models;

public enum AlertType
{
    JavaBackendUnavailable = 1,
    ImportFailure = 2,
    CsvRejectionsFound = 3,
    DrawdownBreach = 4,
    LowSharpeRatio = 5,
    StaleEquityData = 6,
    EmptyReportData = 7
}