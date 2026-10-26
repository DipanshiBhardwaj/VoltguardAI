using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public interface IAuditReportRepository
{
    Task AddAsync(EnergyAuditReport report, CancellationToken cancellationToken = default);

    Task<EnergyAuditReport?> GetOwnedByIdAsync(int auditReportId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<List<EnergyAuditReport>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
