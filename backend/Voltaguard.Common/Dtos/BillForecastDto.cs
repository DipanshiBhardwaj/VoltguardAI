namespace Voltaguard.Common.Dtos;

/// <summary>
/// Projected bill for the month after a meter's latest reading. Deterministic:
/// the average daily use of the most recent readings, scaled to that month and
/// priced against the meter's tariff slabs.
/// </summary>
public sealed class BillForecastDto
{
    // Calendar month being projected, e.g. "2026-06".
    public string ProjectedMonth { get; init; } = string.Empty;
    public int DaysInProjectedMonth { get; init; }

    public decimal AverageDailyKwh { get; init; }
    public decimal ProjectedUnits { get; init; }
    public decimal ProjectedCost { get; init; }
    public IReadOnlyList<SlabChargeDto> SlabCharges { get; init; } = new List<SlabChargeDto>();

    // The readings the average was taken from (UTC instants).
    public DateTimeOffset BasedOnFrom { get; init; }
    public DateTimeOffset BasedOnTo { get; init; }
}
