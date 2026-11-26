using FluentAssertions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class ReadingAnomalyDetectorTests
{
    private static TelemetryLedger Reading(decimal voltage, decimal phaseCurrent = 5.00m, decimal neutralCurrent = 5.00m) => new()
    {
        VoltageV = voltage,
        PhaseCurrentA = phaseCurrent,
        NeutralCurrentA = neutralCurrent
    };

    [Theory]
    [InlineData(230.0)]
    [InlineData(216.2)]   // exactly on the lower edge of the normal band
    [InlineData(243.8)]   // exactly on the upper edge
    public void Detect_VoltageInsideNormalBand_ReturnsNoAnomaly(double voltage)
    {
        var result = ReadingAnomalyDetector.Detect(Reading((decimal)voltage));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(216.1)]
    [InlineData(205.0)]
    [InlineData(100.0)]
    public void Detect_VoltageBelowNormalBand_ReturnsVoltageSag(double voltage)
    {
        var result = ReadingAnomalyDetector.Detect(Reading((decimal)voltage));

        result.Should().Equal(AnomalyType.VoltageSag);
    }

    [Theory]
    [InlineData(243.9)]
    [InlineData(260.0)]
    [InlineData(300.0)]
    public void Detect_VoltageAboveNormalBand_ReturnsVoltageSwell(double voltage)
    {
        var result = ReadingAnomalyDetector.Detect(Reading((decimal)voltage));

        result.Should().Equal(AnomalyType.VoltageSwell);
    }

    [Theory]
    [InlineData(3.63, 1.09)]   // neutral far below phase: bypass
    [InlineData(1.00, 2.50)]   // neutral above phase also counts
    public void Detect_PhaseNeutralGapAboveThreshold_ReturnsCurrentMismatch(double phase, double neutral)
    {
        var result = ReadingAnomalyDetector.Detect(Reading(230m, (decimal)phase, (decimal)neutral));

        result.Should().Equal(AnomalyType.CurrentMismatch);
    }

    [Theory]
    [InlineData(5.00, 4.00)]   // gap exactly at the 1 A threshold is not a mismatch
    [InlineData(5.00, 4.97)]   // rounding noise
    public void Detect_PhaseNeutralGapAtOrBelowThreshold_ReturnsNoAnomaly(double phase, double neutral)
    {
        var result = ReadingAnomalyDetector.Detect(Reading(230m, (decimal)phase, (decimal)neutral));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_SagAndMismatchTogether_ReturnsBoth()
    {
        var result = ReadingAnomalyDetector.Detect(Reading(205m, 10m, 3m));

        result.Should().Equal(AnomalyType.VoltageSag, AnomalyType.CurrentMismatch);
    }
}
