using Microsoft.EntityFrameworkCore;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public class AuditReportRepository : IAuditReportRepository
{
    private readonly VoltaguardDbContext _db;

    public AuditReportRepository(VoltaguardDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(EnergyAuditReport report, CancellationToken cancellationToken = default)
    {
        await _db.EnergyAuditReports.AddAsync(report, cancellationToken);
    }

    // Joins through the meter so a report is only visible to its meter's owner.
    public Task<EnergyAuditReport?> GetOwnedByIdAsync(int auditReportId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        return _db.EnergyAuditReports
            .Include(r => r.Meter)
            .FirstOrDefaultAsync(
                r => r.Id == auditReportId && r.Meter!.OwnerUserId == ownerUserId,
                cancellationToken);
    }

    public Task<List<EnergyAuditReport>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        return _db.EnergyAuditReports
            .Where(r => r.Meter!.OwnerUserId == ownerUserId)
            .OrderByDescending(r => r.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
