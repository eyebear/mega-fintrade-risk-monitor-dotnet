namespace MegaFintradeRiskMonitor.Dtos.Project1;

public class JavaBackendImportAuditDto
{
    public long? Id { get; set; }

    public string? ImportType { get; set; }

    public string? SourceFile { get; set; }

    public string? Status { get; set; }

    public int? TotalRows { get; set; }

    public int? SuccessfulRows { get; set; }

    public int? RejectedRows { get; set; }

    public string? Message { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}