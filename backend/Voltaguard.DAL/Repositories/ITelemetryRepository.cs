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

    /// <summary>Interval timestamps already stored for a meter within an inclusive UTC range.</summary>
    Task<List<DateTimeOffset>> GetExistingIntervalsAsync(
        int meterAssetId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Readings for a meter in an optional inclusive UTC range, newest first,
    /// capped at <paramref name="take"/> rows.
    /// </summary>
    Task<List<TelemetryLedger>> GetRangeByMeterAsync(
        int meterAssetId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Readings outside the normal supply band or with a phase/neutral current gap
    /// above the mismatch threshold, in an optional inclusive UTC range, newest
    /// first. A database-side pre-filter; the BLL anomaly detector classifies them.
    /// </summary>
    Task<List<TelemetryLedger>> GetAbnormalByMeterAsync(
        int meterAssetId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Total number of readings stored for a meter.</summary>
    Task<int> CountByMeterAsync(int meterAssetId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
