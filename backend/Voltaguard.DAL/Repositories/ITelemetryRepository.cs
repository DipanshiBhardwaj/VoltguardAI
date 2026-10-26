using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public interface ITelemetryRepository
{
    Task AddRangeAsync(IEnumerable<TelemetryLedger> readings, CancellationToken cancellationToken = default);

    Task<List<TelemetryLedger>> GetByMeterAndPeriodAsync(
        int meterAssetId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);

    Task<List<TelemetryLedger>> GetRecentByMeterAsync(int meterAssetId, int take, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
