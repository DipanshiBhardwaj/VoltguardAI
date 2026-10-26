using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public interface IMeterRepository
{
    Task<SmartMeterAsset?> GetByIdAsync(int meterAssetId, CancellationToken cancellationToken = default);

    Task<SmartMeterAsset?> GetOwnedByIdAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<List<SmartMeterAsset>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> SerialExistsAsync(string serialNumber, CancellationToken cancellationToken = default);

    Task AddAsync(SmartMeterAsset meter, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
