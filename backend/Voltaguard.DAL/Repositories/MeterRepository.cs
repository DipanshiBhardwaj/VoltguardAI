using Microsoft.EntityFrameworkCore;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public class MeterRepository : IMeterRepository
{
    private readonly VoltaguardDbContext _db;

    public MeterRepository(VoltaguardDbContext db)
    {
        _db = db;
    }

    public Task<SmartMeterAsset?> GetByIdAsync(int meterAssetId, CancellationToken cancellationToken = default)
    {
        return _db.SmartMeterAssets
            .FirstOrDefaultAsync(m => m.Id == meterAssetId, cancellationToken);
    }

    // Scopes the lookup to the owner so one user can never load another's meter by id.
    public Task<SmartMeterAsset?> GetOwnedByIdAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        return _db.SmartMeterAssets
            .FirstOrDefaultAsync(m => m.Id == meterAssetId && m.OwnerUserId == ownerUserId, cancellationToken);
    }

    public Task<List<SmartMeterAsset>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        return _db.SmartMeterAssets
            .Where(m => m.OwnerUserId == ownerUserId)
            .OrderBy(m => m.SerialNumber)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SerialExistsAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        return _db.SmartMeterAssets.AnyAsync(m => m.SerialNumber == serialNumber, cancellationToken);
    }

    public async Task AddAsync(SmartMeterAsset meter, CancellationToken cancellationToken = default)
    {
        await _db.SmartMeterAssets.AddAsync(meter, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
