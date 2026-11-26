using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

public class SmartMeterAssetConfiguration : IEntityTypeConfiguration<SmartMeterAsset>
{
    public void Configure(EntityTypeBuilder<SmartMeterAsset> builder)
    {
        // Built from the enum so a new tariff category cannot be forgotten here.
        builder.ToTable("SmartMeterAsset", table => table.HasCheckConstraint(
            "CK_SmartMeterAsset_SlabCode",
            $"\"SlabCode\" IN ({SqlList.Of(Enum.GetNames<SlabCode>())})"));

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("MeterAssetId");

        builder.Property(m => m.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(m => m.SlabCode).HasConversion<string>().HasMaxLength(50);

        // A serial number is unique among live meters; a soft-deleted one must not
        // block re-registering the same serial later.
        builder.HasIndex(m => m.SerialNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(m => m.Owner)
            .WithMany(u => u.Meters)
            .HasForeignKey(m => m.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
