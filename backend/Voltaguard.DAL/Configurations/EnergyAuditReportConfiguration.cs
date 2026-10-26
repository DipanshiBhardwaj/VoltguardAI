using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

public class EnergyAuditReportConfiguration : IEntityTypeConfiguration<EnergyAuditReport>
{
    public void Configure(EntityTypeBuilder<EnergyAuditReport> builder)
    {
        builder.ToTable("EnergyAuditReport");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("AuditReportId");

        builder.Property(r => r.BillingCyclePeriod).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CalculatedDeterministicCost).HasPrecision(12, 2);
        builder.Property(r => r.TotalUnits).HasPrecision(12, 2);
        builder.Property(r => r.MorningKwh).HasPrecision(12, 2);
        builder.Property(r => r.DayKwh).HasPrecision(12, 2);
        builder.Property(r => r.EveningKwh).HasPrecision(12, 2);
        builder.Property(r => r.NightKwh).HasPrecision(12, 2);
        builder.Property(r => r.HighestDemandKW).HasPrecision(10, 2);
        builder.Property(r => r.AverageDailyKwh).HasPrecision(12, 2);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);

        // Free text written by the AI agent — unbounded and possibly non-ASCII.
        builder.Property(r => r.AiAnalysisFindings).HasColumnType("text");

        builder.HasOne(r => r.Meter)
            .WithMany(m => m.AuditReports)
            .HasForeignKey(r => r.MeterAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
