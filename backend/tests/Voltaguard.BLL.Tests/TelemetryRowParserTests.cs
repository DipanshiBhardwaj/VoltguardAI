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
    public void Parse_BadTimestamp_IsRejectedWithFormatReason()
    {
        var line = "not-a-date,230.5,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Be("Timestamp must be in YYYY-MM-DD HH:mm format.");
    }

    [Theory]
    [InlineData("01/05/2026 18:15")]       // dd/MM/yyyy: a general parser reads this as 5 January
    [InlineData("01-05-2026 18:15")]       // dd-MM-yyyy
    [InlineData("05/01/2026 18:15")]       // MM/dd/yyyy
    [InlineData("2026/05/01 18:15")]
    [InlineData("1 May 2026 18:15")]
    [InlineData("2026-05-01")]             // date without a time
    [InlineData("2026-5-1 18:15")]         // missing leading zeros
    [InlineData("2026-05-01 18:15:00.000")] // fractional seconds
    public void Parse_NonIsoTimestamp_IsRejectedWithFormatReason(string timestamp)
    {
        var line = $"{timestamp},230.5,2.10,9.50,9.45,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Be("Timestamp must be in YYYY-MM-DD HH:mm format.");
    }

    [Theory]
    [InlineData("2026-05-01 18:15", "2026-05-01T12:45:00Z")]
    [InlineData("2026-05-01T18:15", "2026-05-01T12:45:00Z")]
    [InlineData("2026-05-01 18:15:30", "2026-05-01T12:45:30Z")]
    [InlineData("2026-05-01T18:15:00", "2026-05-01T12:45:00Z")]
    [InlineData("2026-05-01T18:15:00Z", "2026-05-01T18:15:00Z")]
    [InlineData("2026-05-01 18:15Z", "2026-05-01T18:15:00Z")]
    [InlineData("2026-05-01T18:15:00+05:30", "2026-05-01T12:45:00Z")]
    [InlineData("2026-05-01T18:15:00-03:00", "2026-05-01T21:15:00Z")]
    public void Parse_IsoTimestamp_IsAcceptedAndStoredInUtc(string timestamp, string expectedUtc)
    {
        var line = $"{timestamp},230.5,2.10,9.50,9.45,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeTrue();
        outcome.Reading!.IntervalEnding.Offset.Should().Be(TimeSpan.Zero);
        outcome.Reading.IntervalEnding.Should().Be(DateTimeOffset.Parse(expectedUtc));
    }

    [Theory]
    [InlineData("2026-05-09 25:70")]       // hour and minute out of range
    [InlineData("2026-13-01 10:00")]       // month 13
    [InlineData("2026-02-30 10:00")]       // no 30 February
    [InlineData("2026-05-01T10:00+15:00")] // no real offset is this large
    public void Parse_IsoShapedButImpossibleTimestamp_IsRejected(string timestamp)
    {
        var line = $"{timestamp},230.5,2.10,9.50,9.45,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeFalse();
        outcome.Reason.Should().Be("Column 1 is not a valid date and time.");
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

    [Theory]
    [InlineData("2026-05-01 18:15")]
    [InlineData("2026-05-01T18:15:00")]
    public void Parse_TimestampWithoutOffset_IsTreatedAsIstAndStoredInUtc(string timestamp)
    {
        var line = $"{timestamp},230.5,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeTrue();
        outcome.Reading!.IntervalEnding.Offset.Should().Be(TimeSpan.Zero);
        outcome.Reading.IntervalEnding.Should().Be(new DateTimeOffset(2026, 5, 1, 12, 45, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Parse_TimestampEndingInZ_IsKeptAsUtc()
    {
        var outcome = TelemetryRowParser.Parse(ValidLine, MeterId);

        outcome.IsAccepted.Should().BeTrue();
        outcome.Reading!.IntervalEnding.Offset.Should().Be(TimeSpan.Zero);
        outcome.Reading.IntervalEnding.Should().Be(new DateTimeOffset(2026, 5, 1, 18, 15, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Parse_TimestampWithIstOffset_IsRespectedAndStoredInUtc()
    {
        var line = "2026-05-01T18:15:00+05:30,230.5,2.10,9.50,0.20,0.95,50.00,1234.75";

        var outcome = TelemetryRowParser.Parse(line, MeterId);

        outcome.IsAccepted.Should().BeTrue();
        outcome.Reading!.IntervalEnding.Offset.Should().Be(TimeSpan.Zero);
        outcome.Reading.IntervalEnding.Should().Be(new DateTimeOffset(2026, 5, 1, 12, 45, 0, TimeSpan.Zero));
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
