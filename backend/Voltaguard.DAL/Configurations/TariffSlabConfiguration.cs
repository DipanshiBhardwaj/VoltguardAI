using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Configurations;

public class TariffSlabConfiguration : IEntityTypeConfiguration<TariffSlab>
{
    public void Configure(EntityTypeBuilder<TariffSlab> builder)
    {
        builder.ToTable("TariffSlab");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("TariffSlabId");

        builder.Property(s => s.SlabCode).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.FromUnit).HasPrecision(10, 2);
        builder.Property(s => s.ToUnit).HasPrecision(10, 2);
        builder.Property(s => s.RatePerUnit).HasPrecision(8, 4);
        builder.Property(s => s.ToDMultiplier).HasPrecision(4, 2);
        builder.Property(s => s.FixedCharge).HasPrecision(10, 2);

        builder.HasIndex(s => s.SlabCode);
    }
}
