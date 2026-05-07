using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaFintradeRiskMonitor.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoringSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JavaBackendReachable = table.Column<bool>(type: "INTEGER", nullable: false),
                    JavaBackendBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ReportSummaryAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImportAuditAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImportRejectionsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    PortfolioMonitoringAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    SymbolMonitoringAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    SymbolCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CriticalAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HighAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LowAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskAlerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SourceEndpoint = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SourceValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ThresholdValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringSnapshots_CreatedAtUtc",
                table: "MonitoringSnapshots",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringSnapshots_JavaBackendReachable",
                table: "MonitoringSnapshots",
                column: "JavaBackendReachable");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringSnapshots_Status",
                table: "MonitoringSnapshots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_CreatedAtUtc",
                table: "RiskAlerts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_IsActive",
                table: "RiskAlerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_Symbol",
                table: "RiskAlerts",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAlerts_Type_Symbol_SourceEndpoint_IsActive",
                table: "RiskAlerts",
                columns: new[] { "Type", "Symbol", "SourceEndpoint", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoringSnapshots");

            migrationBuilder.DropTable(
                name: "RiskAlerts");
        }
    }
}
