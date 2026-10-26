using FluentAssertions;
using Voltaguard.BLL.Services;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class TelemetryRowParserTests
{
    private const int MeterId = 1;

    // Columns: IntervalEnding, Voltage, ActivePower, PhaseCurrent, NeutralCurrent, PowerFactor, Frequency, CumulativeKWH
    private const string ValidLine = "2026-05-01T18:15:00Z,230.5,2.10,9.50,0.20,0.95,50.00,1234.75";

    [Fact]
    public void Parse_ValidLine_IsAccepted()
    {
        var outcome = TelemetryRowParser.Parse(ValidLine, MeterId);

        outcome.IsAccepted.Should().BeTrue();
        outcome.Reading!.VoltageV.Should().Be(230.5m);
        outcome.Reading.MeterAssetId.Should().Be(MeterId);
    }

    [Fact]
    public void Parse_WrongColumnCount_IsRejected()
    {
        var outcome = TelemetryRowParser.Parse("2026-05-01T18:15:00Z,230.5,2.10", MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Contain("columns");
    }

    [Fact]
    public void Parse_BadTimestamp_IsRejected()
    {
        var line = "not-a-date,230.5,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Contain("timestamp");
    }

    [Fact]
    public void Parse_NonNumericReading_IsRejected()
    {
        var line = "2026-05-01T18:15:00Z,abc,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Contain("numeric");
    }

    [Fact]
    public void Parse_VoltageOutsidePhysicalRange_IsRejected()
    {
        // 999 V is impossible on a domestic single-phase line.
        var line = "2026-05-01T18:15:00Z,999,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Contain("Voltage");
    }

    [Fact]
    public void Parse_FrequencyOutsideRange_IsRejected()
    {
        var line = "2026-05-01T18:15:00Z,230,2.10,9.50,0.20,0.95,60.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Contain("Frequency");
    }
}
