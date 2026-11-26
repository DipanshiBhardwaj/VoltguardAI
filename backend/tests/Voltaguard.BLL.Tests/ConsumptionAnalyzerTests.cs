using FluentAssertions;
using Voltaguard.BLL.Services;
using Voltaguard.DAL.Entities;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class ConsumptionAnalyzerTests
{
    // Readings are stored in UTC, exactly as the parser produces them.
    private static TelemetryLedger Reading(string isoTime, decimal activePowerKw) => new()
    {
        IntervalEnding = DateTimeOffset.Parse(isoTime).ToUniversalTime(),
        ActivePowerKW = activePowerKw
    };

    [Fact]
    public void Analyze_NoReadings_ReturnsEmptyFacts()
    {
        var facts = ConsumptionAnalyzer.Analyze(new List<TelemetryLedger>());

        facts.Should().BeSameAs(ConsumptionFacts.Empty);
        facts.HighestDemandOn.Should().BeNull();
    }

    [Fact]
    public void Analyze_OneReadingPerIstBand_SplitsEnergyIntoCorrectBands()
    {
        // One reading in each band; 4 kW over a 15-min slice = 1 kWh each.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00+05:30", 4m),  // morning
            Reading("2026-05-01T14:00:00+05:30", 4m),  // daytime
            Reading("2026-05-01T19:00:00+05:30", 4m),  // evening
            Reading("2026-05-01T02:00:00+05:30", 4m),  // night
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.MorningKwh.Should().Be(1m);
        facts.DayKwh.Should().Be(1m);
        facts.EveningKwh.Should().Be(1m);
        facts.NightKwh.Should().Be(1m);
    }

    [Fact]
    public void Analyze_UtcTimestamps_BandsByIstHour()
    {
        // 03:00Z is 08:30 IST (morning), 13:00Z is 18:30 IST (evening). Read as UTC
        // hours they would wrongly land in night and daytime.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T03:00:00Z", 4m),
            Reading("2026-05-01T13:00:00Z", 4m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.MorningKwh.Should().Be(1m);
        facts.EveningKwh.Should().Be(1m);
        facts.NightKwh.Should().Be(0m);
        facts.DayKwh.Should().Be(0m);
    }

    [Fact]
    public void Analyze_SeveralReadingsInOneHour_FindsBusiestIstHour()
    {
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00+05:30", 1m),
            Reading("2026-05-01T19:00:00+05:30", 5m),  // heaviest
            Reading("2026-05-01T19:15:00+05:30", 4m),  // same hour, adds to it
            Reading("2026-05-01T22:00:00+05:30", 2m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.PeakHour.Should().Be(19);
    }

    [Fact]
    public void Analyze_UtcTimestamps_ReportsPeakHourInIst()
    {
        // 14:30Z = 20:00 IST.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T14:30:00Z", 5m),
            Reading("2026-05-01T02:30:00Z", 1m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.PeakHour.Should().Be(20);
    }

    [Fact]
    public void Analyze_SpikeInReadings_CapturesHighestSingleDrawAndTimestamp()
    {
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00+05:30", 2m),
            Reading("2026-05-01T20:30:00+05:30", 7.5m),  // the spike
            Reading("2026-05-01T22:00:00+05:30", 3m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.HighestDemandKW.Should().Be(7.5m);
        facts.HighestDemandOn.Should().Be(DateTimeOffset.Parse("2026-05-01T15:00:00Z"));
    }

    [Fact]
    public void Analyze_TwoIstDays_AveragesDailyUseAcrossDistinctDays()
    {
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00+05:30", 2m),
            Reading("2026-05-01T20:00:00+05:30", 2m),
            Reading("2026-05-02T08:00:00+05:30", 2m),
            Reading("2026-05-02T20:00:00+05:30", 2m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        // total = 4 slices * (2 kW * 0.25h) = 2 kWh over 2 days = 1 kWh/day.
        facts.AverageDailyKwh.Should().Be(1m);
    }

    [Fact]
    public void Analyze_UtcDatesSpanningMidnight_CountsDaysOnIstCalendar()
    {
        // 30 Apr 20:00Z and 1 May 02:00Z are both on 1 May in IST (01:30 and 07:30),
        // so this is one day, not two.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-04-30T20:00:00Z", 2m),
            Reading("2026-05-01T02:00:00Z", 2m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        // 2 slices * 0.5 kWh = 1 kWh over 1 day.
        facts.AverageDailyKwh.Should().Be(1m);
    }
}
