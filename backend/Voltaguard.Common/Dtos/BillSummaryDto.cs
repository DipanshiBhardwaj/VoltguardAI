namespace Voltaguard.Common.Dtos;

/// <summary>
/// Result of the deterministic billing run for one meter over one period. The
/// per-slab breakdown lets the frontend show exactly how the total was reached.
/// </summary>
public sealed class BillSummaryDto
{
    public int AuditReportId { get; init; }
    public string BillingCyclePeriod { get; init; } = string.Empty;
    public decimal TotalUnits { get; init; }
    public decimal TotalCost { get; init; }
    public IReadOnlyList<SlabChargeDto> SlabCharges { get; init; } = new List<SlabChargeDto>();
    public ConsumptionFactsDto Facts { get; init; } = new();
}

/// <summary>Units and cost charged within one tariff slab.</summary>
public sealed class SlabChargeDto
{
    public decimal FromUnit { get; init; }
    public decimal? ToUnit { get; init; }
    public decimal RatePerUnit { get; init; }
    public decimal UnitsCharged { get; init; }
    public decimal Charge { get; init; }
}
