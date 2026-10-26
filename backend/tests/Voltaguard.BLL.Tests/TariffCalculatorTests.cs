using FluentAssertions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class TariffCalculatorTests
{
    // Two-slab domestic tariff: first 125 units cheap, everything above dearer.
    private static List<TariffSlab> StandardSlabs() => new()
    {
        new TariffSlab { SlabCode = SlabCode.DomesticStandard, FromUnit = 0, ToUnit = 125, RatePerUnit = 4.00m, ToDMultiplier = 1.0m, FixedCharge = 0 },
        new TariffSlab { SlabCode = SlabCode.DomesticStandard, FromUnit = 125, ToUnit = null, RatePerUnit = 6.00m, ToDMultiplier = 1.0m, FixedCharge = 0 }
    };

    [Fact]
    public void Calculate_UsageWithinFirstSlab_ChargesOnlyFirstRate()
    {
        var result = TariffCalculator.Calculate(100m, StandardSlabs());

        result.TotalCost.Should().Be(400.00m);
        result.SlabCharges.Should().HaveCount(1);
    }

    [Fact]
    public void Calculate_UsageSpansTwoSlabs_ChargesEachPortionAtItsRate()
    {
        // 200 units = 125 @ 4.00 (500) + 75 @ 6.00 (450) = 950.
        var result = TariffCalculator.Calculate(200m, StandardSlabs());

        result.TotalCost.Should().Be(950.00m);
        result.SlabCharges.Should().HaveCount(2);
    }

    [Fact]
    public void Calculate_ExactlyAtSlabBoundary_DoesNotEnterNextSlab()
    {
        var result = TariffCalculator.Calculate(125m, StandardSlabs());

        result.TotalCost.Should().Be(500.00m);
        result.SlabCharges.Should().HaveCount(1);
    }

    [Fact]
    public void Calculate_WithToDMultiplier_ScalesThatSlabCharge()
    {
        var slabs = new List<TariffSlab>
        {
            new() { SlabCode = SlabCode.DomesticStandard, FromUnit = 0, ToUnit = null, RatePerUnit = 5.00m, ToDMultiplier = 1.2m, FixedCharge = 0 }
        };

        // 100 units * 5.00 * 1.2 = 600.
        var result = TariffCalculator.Calculate(100m, slabs);

        result.TotalCost.Should().Be(600.00m);
    }

    [Fact]
    public void Calculate_WithFixedCharge_AddsItOncePerSlabUsed()
    {
        var slabs = new List<TariffSlab>
        {
            new() { SlabCode = SlabCode.DomesticStandard, FromUnit = 0, ToUnit = null, RatePerUnit = 5.00m, ToDMultiplier = 1.0m, FixedCharge = 50 }
        };

        // 100 * 5.00 + 50 fixed = 550.
        var result = TariffCalculator.Calculate(100m, slabs);

        result.TotalCost.Should().Be(550.00m);
    }

    [Fact]
    public void Calculate_ZeroUsage_ReturnsZero()
    {
        var result = TariffCalculator.Calculate(0m, StandardSlabs());

        result.TotalCost.Should().Be(0m);
        result.SlabCharges.Should().BeEmpty();
    }
}
