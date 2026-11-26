using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL;

/// <summary>
/// The single point of contact with PostgreSQL. Extends IdentityDbContext so the
/// Identity tables live alongside the domain tables, applies every entity
/// configuration, and enforces soft delete through a global query filter.
/// </summary>
public class VoltaguardDbContext : IdentityDbContext<UserAccount, IdentityRole<int>, int>
{
    public VoltaguardDbContext(DbContextOptions<VoltaguardDbContext> options)
        : base(options)
    {
    }

    public DbSet<SmartMeterAsset> SmartMeterAssets => Set<SmartMeterAsset>();
    public DbSet<TelemetryLedger> TelemetryLedger => Set<TelemetryLedger>();
    public DbSet<EnergyAuditReport> EnergyAuditReports => Set<EnergyAuditReport>();
    public DbSet<TariffSlab> TariffSlabs => Set<TariffSlab>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(VoltaguardDbContext).Assembly);

        // Soft delete is the only global filter in this schema.
        builder.Entity<SmartMeterAsset>().HasQueryFilter(m => !m.IsDeleted);
        builder.Entity<EnergyAuditReport>().HasQueryFilter(r => !r.IsDeleted);
        builder.Entity<TariffSlab>().HasQueryFilter(s => !s.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditRules();
        return base.SaveChanges();
    }

    // CreatedOn is stamped once on insert; a delete is turned into a soft delete.
    private void ApplyAuditRules()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }
        }
    }
}
