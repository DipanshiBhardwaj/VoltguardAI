using System.Globalization;
using Voltaguard.Common.Constants;
using Voltaguard.Common.Dtos;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

/// <summary>
/// Predictive cost report. Takes the average daily use over the latest
/// <see cref="QueryLimitConstants.ForecastWindowDays"/> days of readings, scales it
/// to the next calendar month and prices it with the same
/// <see cref="TariffCalculator"/> the real bill uses. No statistics or AI: the
/// projection is fully reproducible by hand.
/// </summary>
public class ForecastService : IForecastService
{
    private readonly IMeterRepository _meterRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ITariffRepository _tariffRepository;

    public ForecastService(
        IMeterRepository meterRepository,
        ITelemetryRepository telemetryRepository,
        ITariffRepository tariffRepository)
    {
        _meterRepository = meterRepository;
        _telemetryRepository = telemetryRepository;
        _tariffRepository = tariffRepository;
    }

    public async Task<BillForecastDto> GetForecastAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Meter not found for the current user.");

        var latest = await _telemetryRepository.GetRecentByMeterAsync(meterAssetId, 1, cancellationToken);
        if (latest.Count == 0)
        {
            throw new InvalidOperationException("This meter has no readings to project a bill from.");
        }

        var windowEnd = latest[0].IntervalEnding.ToUniversalTime();
        var windowStart = windowEnd.AddDays(-QueryLimitConstants.ForecastWindowDays);
        var readings = await _telemetryRepository.GetByMeterAndPeriodAsync(meterAssetId, windowStart, windowEnd, cancellationToken);

        // Average over the time the readings actually span, from the meter's own
        // odometer (the same source the bill uses), not over the nominal window.
        var spanDays = readings.Count < 2
            ? 0m
            : (decimal)(readings[^1].IntervalEnding - readings[0].IntervalEnding).TotalDays;
        if (spanDays < 1m)
        {
            throw new InvalidOperationException("At least one full day of readings is needed to project a bill.");
        }

        var unitsInWindow = Math.Max(0m, readings[^1].CumulativeKWH - readings[0].CumulativeKWH);
        var averageDailyKwh = unitsInWindow / spanDays;

        // The month after the latest reading, on the Indian calendar.
        var latestIst = windowEnd.ToOffset(TimeZoneConstants.IstOffset);
        var projectedMonth = new DateTime(latestIst.Year, latestIst.Month, 1).AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(projectedMonth.Year, projectedMonth.Month);

        var projectedUnits = decimal.Round(averageDailyKwh * daysInMonth, 2);
        var slabs = await _tariffRepository.GetBySlabCodeAsync(meter.SlabCode, cancellationToken);
        var breakdown = TariffCalculator.Calculate(projectedUnits, slabs);

        return new BillForecastDto
        {
            ProjectedMonth = projectedMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            DaysInProjectedMonth = daysInMonth,
            AverageDailyKwh = decimal.Round(averageDailyKwh, 2),
            ProjectedUnits = projectedUnits,
            ProjectedCost = breakdown.TotalCost,
            SlabCharges = breakdown.SlabCharges,
            BasedOnFrom = readings[0].IntervalEnding,
            BasedOnTo = readings[^1].IntervalEnding
        };
    }
}
