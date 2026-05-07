using MegaFintradeRiskMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaFintradeRiskMonitor.Data;

public class RiskMonitorDbContext : DbContext
{
    public RiskMonitorDbContext(DbContextOptions<RiskMonitorDbContext> options)
        : base(options)
    {
    }

    public DbSet<RiskAlert> RiskAlerts => Set<RiskAlert>();

    public DbSet<MonitoringSnapshot> MonitoringSnapshots => Set<MonitoringSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RiskAlert>(entity =>
        {
            entity.HasKey(alert => alert.Id);

            entity.Property(alert => alert.Symbol)
                .HasMaxLength(32);

            entity.Property(alert => alert.Type)
                .IsRequired();

            entity.Property(alert => alert.Severity)
                .IsRequired();

            entity.Property(alert => alert.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(alert => alert.SourceEndpoint)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(alert => alert.SourceValue)
                .HasMaxLength(200);

            entity.Property(alert => alert.ThresholdValue)
                .HasMaxLength(200);

            entity.Property(alert => alert.IsActive)
                .IsRequired();

            entity.Property(alert => alert.CreatedAtUtc)
                .IsRequired();

            entity.Property(alert => alert.ResolvedAtUtc);

            entity.HasIndex(alert => alert.IsActive);

            entity.HasIndex(alert => alert.CreatedAtUtc);

            entity.HasIndex(alert => alert.Symbol);

            entity.HasIndex(alert => new
            {
                alert.Type,
                alert.Symbol,
                alert.SourceEndpoint,
                alert.IsActive
            });
        });

        modelBuilder.Entity<MonitoringSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);

            entity.Property(snapshot => snapshot.JavaBackendReachable)
                .IsRequired();

            entity.Property(snapshot => snapshot.JavaBackendBaseUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(snapshot => snapshot.ReportSummaryAvailable)
                .IsRequired();

            entity.Property(snapshot => snapshot.ImportAuditAvailable)
                .IsRequired();

            entity.Property(snapshot => snapshot.ImportRejectionsAvailable)
                .IsRequired();

            entity.Property(snapshot => snapshot.PortfolioMonitoringAvailable)
                .IsRequired();

            entity.Property(snapshot => snapshot.SymbolMonitoringAvailable)
                .IsRequired();

            entity.Property(snapshot => snapshot.SymbolCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.ActiveAlertCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.CriticalAlertCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.HighAlertCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.MediumAlertCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.LowAlertCount)
                .IsRequired();

            entity.Property(snapshot => snapshot.Status)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(snapshot => snapshot.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(snapshot => snapshot.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(snapshot => snapshot.CreatedAtUtc);

            entity.HasIndex(snapshot => snapshot.Status);

            entity.HasIndex(snapshot => snapshot.JavaBackendReachable);
        });
    }
}