using Microsoft.EntityFrameworkCore;
using Voltaguard.Common.Constants;
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

    public Task<List<DateTimeOffset>> GetExistingIntervalsAsync(
        int meterAssetId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.TelemetryLedger
            .Where(t => t.MeterAssetId == meterAssetId
                        && t.IntervalEnding >= fromUtc
                        && t.IntervalEnding <= toUtc)
            .Select(t => t.IntervalEnding)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TelemetryLedger>> GetRangeByMeterAsync(
        int meterAssetId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        return InRange(meterAssetId, fromUtc, toUtc)
            .OrderByDescending(t => t.IntervalEnding)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TelemetryLedger>> GetAbnormalByMeterAsync(
        int meterAssetId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
    {
        return InRange(meterAssetId, fromUtc, toUtc)
            .Where(t => t.VoltageV < ValidationLimitConstants.NormalMinVoltage
                        || t.VoltageV > ValidationLimitConstants.NormalMaxVoltage
                        || Math.Abs(t.PhaseCurrentA - t.NeutralCurrentA) > ValidationLimitConstants.CurrentMismatchThresholdA)
            .OrderByDescending(t => t.IntervalEnding)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<TelemetryLedger> InRange(int meterAssetId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        var query = _db.TelemetryLedger.Where(t => t.MeterAssetId == meterAssetId);
        if (fromUtc is { } from)
        {
            query = query.Where(t => t.IntervalEnding >= from);
        }
        if (toUtc is { } to)
        {
            query = query.Where(t => t.IntervalEnding <= to);
        }
        return query;
    }

    public Task<int> CountByMeterAsync(int meterAssetId, CancellationToken cancellationToken = default)
    {
        return _db.TelemetryLedger.CountAsync(t => t.MeterAssetId == meterAssetId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
