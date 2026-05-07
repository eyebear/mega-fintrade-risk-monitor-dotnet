namespace MegaFintradeRiskMonitor.Dtos.Project1;

public class JavaBackendImportRejectionDto
{
    public long? Id { get; set; }

    public string? ImportType { get; set; }

    public string? SourceFile { get; set; }

    public int? RowNumber { get; set; }

    public string? RawRow { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAtUtc { get; set; }
}