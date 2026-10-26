using Microsoft.EntityFrameworkCore;
using Voltaguard.DAL.Entities;

namespace Voltaguard.DAL.Repositories;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly VoltaguardDbContext _db;

    public TelemetryRepository(VoltaguardDbContext db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(IEnumerable<TelemetryLedger> readings, CancellationToken cancellationToken = default)
    {
        await _db.TelemetryLedger.AddRangeAsync(readings, cancellationToken);
    }

    public Task<List<TelemetryLedger>> GetByMeterAndPeriodAsync(
        int meterAssetId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return _db.TelemetryLedger
            .Where(t => t.MeterAssetId == meterAssetId
                        && t.IntervalEnding >= fromDate
                        && t.IntervalEnding <= toDate)
            .OrderBy(t => t.IntervalEnding)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TelemetryLedger>> GetRecentByMeterAsync(int meterAssetId, int take, CancellationToken cancellationToken = default)
    {
        return _db.TelemetryLedger
            .Where(t => t.MeterAssetId == meterAssetId)
            .OrderByDescending(t => t.IntervalEnding)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
