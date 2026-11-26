using Voltaguard.Common.Enums;

namespace Voltaguard.Common.Dtos;

/// <summary>
/// One entry in the anomaly log: a stored reading and the way it is abnormal.
/// A reading that is abnormal in two ways appears once per type.
/// </summary>
public sealed class AnomalyReadingDto
{
    public long LogId { get; init; }
    public DateTimeOffset IntervalEnding { get; init; }
    public AnomalyType Type { get; init; }
    public decimal VoltageV { get; init; }
    public decimal PhaseCurrentA { get; init; }
    public decimal NeutralCurrentA { get; init; }
}
