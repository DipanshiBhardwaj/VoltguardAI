using Voltaguard.Common.Enums;

namespace Voltaguard.DAL.Entities;

/// <summary>
/// One tier in a domestic tariff. Slabs are seeded and read by the billing engine:
/// consumption is split across the slabs that cover it, charged at each slab's rate,
/// with a Time-of-Day multiplier and a fixed charge applied per slab.
/// </summary>
public class TariffSlab : BaseEntity
{
    public SlabCode SlabCode { get; set; }

    public decimal FromUnit { get; set; }

    // Null means the top, open-ended slab.
    public decimal? ToUnit { get; set; }

    public decimal RatePerUnit { get; set; }

    public decimal ToDMultiplier { get; set; } = 1.0m;

    public decimal FixedCharge { get; set; }
}
