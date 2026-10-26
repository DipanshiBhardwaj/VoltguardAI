using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

public class TelemetryLedgerConfiguration : IEntityTypeConfiguration<TelemetryLedger>
{
    public void Configure(EntityTypeBuilder<TelemetryLedger> builder)
    {
        builder.ToTable("TelemetryLedger");

        builder.HasKey(t => t.LogId);

        builder.Property(t => t.VoltageV).HasPrecision(6, 2);
        builder.Property(t => t.ActivePowerKW).HasPrecision(10, 2);
        builder.Property(t => t.PhaseCurrentA).HasPrecision(10, 2);
        builder.Property(t => t.NeutralCurrentA).HasPrecision(10, 2);
        builder.Property(t => t.PowerFactor).HasPrecision(3, 2);
        builder.Property(t => t.FrequencyHz).HasPrecision(4, 2);
        builder.Property(t => t.CumulativeKWH).HasPrecision(14, 2);

        // Answers "readings for this meter in this period, newest first" with an
        // index-only scan — the billing query and the charts both rely on it.
        builder.HasIndex(t => new { t.MeterAssetId, t.IntervalEnding });

        builder.HasOne(t => t.Meter)
            .WithMany(m => m.Readings)
            .HasForeignKey(t => t.MeterAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
