namespace Voltaguard.Common.Dtos;

/// <summary>
/// Measured facts about when and how heavily energy was used in a period. Computed
/// from the 15-minute readings, not estimated — this is what the AI explanation is
/// grounded on and what the dashboard charts show.
/// </summary>
public sealed class ConsumptionFactsDto
{
    /// <summary>Hour of day (0–23) with the highest total consumption.</summary>
    public int PeakHour { get; init; }
    public decimal MorningKwh { get; init; }
    public decimal DayKwh { get; init; }
    public decimal EveningKwh { get; init; }
    public decimal NightKwh { get; init; }
    public decimal HighestDemandKW { get; init; }
    public DateTimeOffset? HighestDemandOn { get; init; }
    public decimal AverageDailyKwh { get; init; }
}
