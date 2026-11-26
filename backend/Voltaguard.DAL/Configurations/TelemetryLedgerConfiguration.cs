using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Voltaguard.Common.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

public class TelemetryLedgerConfiguration : IEntityTypeConfiguration<TelemetryLedger>
{
    public void Configure(EntityTypeBuilder<TelemetryLedger> builder)
    {
        // The same physical limits the ingestion parser enforces, so a bad reading
        // cannot reach the table by any other path either.
        builder.ToTable("TelemetryLedger", table =>
        {
            table.HasCheckConstraint(
                "CK_TelemetryLedger_VoltageV",
                string.Create(CultureInfo.InvariantCulture,
                    $"\"VoltageV\" BETWEEN {ValidationLimitConstants.MinVoltage} AND {ValidationLimitConstants.MaxVoltage}"));
            table.HasCheckConstraint(
                "CK_TelemetryLedger_PowerFactor",
                string.Create(CultureInfo.InvariantCulture,
                    $"\"PowerFactor\" BETWEEN {ValidationLimitConstants.MinPowerFactor} AND {ValidationLimitConstants.MaxPowerFactor}"));
            table.HasCheckConstraint(
                "CK_TelemetryLedger_FrequencyHz",
                string.Create(CultureInfo.InvariantCulture,
                    $"\"FrequencyHz\" BETWEEN {ValidationLimitConstants.MinFrequency} AND {ValidationLimitConstants.MaxFrequency}"));
        });

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
        // Unique so the same 15-minute interval can never be stored twice for a
        // meter (re-uploading a file would otherwise double every total).
        builder.HasIndex(t => new { t.MeterAssetId, t.IntervalEnding }).IsUnique();

        builder.HasOne(t => t.Meter)
            .WithMany(m => m.Readings)
            .HasForeignKey(t => t.MeterAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
