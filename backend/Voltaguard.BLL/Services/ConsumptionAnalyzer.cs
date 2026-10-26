using Voltaguard.DAL.Entities;

namespace Voltaguard.BLL.Services;

/// <summary>
/// Derives real, defensible facts from a meter's 15-minute readings — when energy
/// was used, how it split across the day, and the single largest draw. These are
/// measured from the data, not guessed, and are what the AI explanation is grounded
/// on. A whole-house meter cannot identify individual appliances, so this never
/// claims to; it reports timing and magnitude, which the data does prove.
/// </summary>
public static class ConsumptionAnalyzer
{
    // Time-of-day bands (local clock hours). Evening is when domestic peak tariffs
    // and heavy appliance use typically land.
    private const int MorningStart = 6;   // 06:00–11:59
    private const int DayStart = 12;      // 12:00–16:59
    private const int EveningStart = 17;  // 17:00–22:59
    // Night is everything else (23:00–05:59).

    public static ConsumptionFacts Analyze(IReadOnlyList<TelemetryLedger> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        if (readings.Count == 0)
        {
            return ConsumptionFacts.Empty;
        }

        // Each reading covers a 15-minute slice, so energy for a slice is
        // power(kW) × 0.25h. Summing these approximates the kWh in each band.
        decimal morning = 0m, day = 0m, evening = 0m, night = 0m;
        var hourTotals = new decimal[24];

        TelemetryLedger peak = readings[0];

        foreach (var r in readings)
        {
            var sliceKwh = r.ActivePowerKW * 0.25m;
            var hour = r.IntervalEnding.Hour;
            hourTotals[hour] += sliceKwh;

            if (hour is >= MorningStart and < DayStart) morning += sliceKwh;
            else if (hour is >= DayStart and < EveningStart) day += sliceKwh;
            else if (hour is >= EveningStart and < 23) evening += sliceKwh;
            else night += sliceKwh;

            if (r.ActivePowerKW > peak.ActivePowerKW)
            {
                peak = r;
            }
        }

        // The hour of day with the highest summed energy.
        var peakHour = 0;
        for (var h = 1; h < 24; h++)
        {
            if (hourTotals[h] > hourTotals[peakHour])
            {
                peakHour = h;
            }
        }

        var distinctDays = readings
            .Select(r => r.IntervalEnding.Date)
            .Distinct()
            .Count();

        var totalBandKwh = morning + day + evening + night;
        var averageDailyKwh = distinctDays > 0 ? totalBandKwh / distinctDays : 0m;

        return new ConsumptionFacts
        {
            PeakHour = peakHour,
            MorningKwh = decimal.Round(morning, 2),
            DayKwh = decimal.Round(day, 2),
            EveningKwh = decimal.Round(evening, 2),
            NightKwh = decimal.Round(night, 2),
            HighestDemandKW = decimal.Round(peak.ActivePowerKW, 2),
            HighestDemandOn = peak.IntervalEnding,
            AverageDailyKwh = decimal.Round(averageDailyKwh, 2)
        };
    }
}

/// <summary>Measured consumption facts for one audited period.</summary>
public sealed class ConsumptionFacts
{
    public int PeakHour { get; init; }
    public decimal MorningKwh { get; init; }
    public decimal DayKwh { get; init; }
    public decimal EveningKwh { get; init; }
    public decimal NightKwh { get; init; }
    public decimal HighestDemandKW { get; init; }
    public DateTimeOffset? HighestDemandOn { get; init; }
    public decimal AverageDailyKwh { get; init; }

    public static ConsumptionFacts Empty { get; } = new();
}
