namespace Voltaguard.Common.Dtos;

/// <summary>One 15-minute meter reading as shown on the consumption charts.</summary>
public sealed class TelemetryRowDto
{
    public long LogId { get; init; }
    public DateTimeOffset IntervalEnding { get; init; }
    public decimal VoltageV { get; init; }
    public decimal ActivePowerKW { get; init; }
    public decimal PowerFactor { get; init; }
    public decimal FrequencyHz { get; init; }
    public decimal CumulativeKWH { get; init; }
}
