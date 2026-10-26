using Voltaguard.Common.Dtos;
using Voltaguard.DAL.Entities;

namespace Voltaguard.BLL.Services;

/// <summary>
/// Pure, deterministic slab math. Splits total consumption across the tariff slabs
/// that cover it and charges each portion at that slab's rate, Time-of-Day multiplier
/// and fixed charge. No database, no AI — the same input always gives the same bill.
/// </summary>
public static class TariffCalculator
{
    public static BillBreakdown Calculate(decimal totalUnits, IReadOnlyList<TariffSlab> slabs)
    {
        ArgumentNullException.ThrowIfNull(slabs);

        var charges = new List<SlabChargeDto>();
        decimal totalCost = 0m;

        foreach (var slab in slabs)
        {
            // Width of this slab; the top slab is open-ended.
            var slabFloor = slab.FromUnit;
            var slabCeiling = slab.ToUnit ?? decimal.MaxValue;

            if (totalUnits <= slabFloor)
            {
                continue;
            }

            var unitsInSlab = Math.Min(totalUnits, slabCeiling) - slabFloor;
            if (unitsInSlab <= 0m)
            {
                continue;
            }

            var charge = (unitsInSlab * slab.RatePerUnit * slab.ToDMultiplier) + slab.FixedCharge;
            totalCost += charge;

            charges.Add(new SlabChargeDto
            {
                FromUnit = slab.FromUnit,
                ToUnit = slab.ToUnit,
                RatePerUnit = slab.RatePerUnit,
                UnitsCharged = decimal.Round(unitsInSlab, 2),
                Charge = decimal.Round(charge, 2)
            });
        }

        return new BillBreakdown(decimal.Round(totalCost, 2), charges);
    }
}

/// <summary>Total cost plus the per-slab charges that make it up.</summary>
public sealed record BillBreakdown(decimal TotalCost, IReadOnlyList<SlabChargeDto> SlabCharges);
