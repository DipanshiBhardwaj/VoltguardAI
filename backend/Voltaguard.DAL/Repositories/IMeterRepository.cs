using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public interface IMeterRepository
{
    Task<SmartMeterAsset?> GetByIdAsync(int meterAssetId, CancellationToken cancellationToken = default);

    Task<SmartMeterAsset?> GetOwnedByIdAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<List<SmartMeterAsset>> GetByOwnerAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> SerialExistsAsync(string serialNumber, CancellationToken cancellationToken = default);

    /// <summary>True when a live meter other than <paramref name="excludeMeterAssetId"/> uses this serial.</summary>
    Task<bool> SerialExistsForOtherMeterAsync(string serialNumber, int excludeMeterAssetId, CancellationToken cancellationToken = default);

    Task AddAsync(SmartMeterAsset meter, CancellationToken cancellationToken = default);

    /// <summary>Marks a meter for removal; the DbContext turns this into a soft delete on save.</summary>
    void Remove(SmartMeterAsset meter);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
