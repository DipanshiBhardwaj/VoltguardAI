using Voltaguard.Common.Enums;

namespace Voltaguard.Common.Dtos;

/// <summary>A smart meter belonging to the signed-in user.</summary>
public sealed class MeterDto
{
    public int MeterAssetId { get; init; }
    public string SerialNumber { get; init; } = string.Empty;
    public SlabCode SlabCode { get; init; }
    public int ReadingCount { get; init; }
    public DateTimeOffset? LatestReadingOn { get; init; }
}
