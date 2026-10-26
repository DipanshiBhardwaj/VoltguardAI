using FluentAssertions;
using Voltaguard.BLL.Services;
using Voltaguard.DAL.Entities;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class ConsumptionAnalyzerTests
{
    private static TelemetryLedger Reading(string isoTime, decimal activePowerKw) => new()
    {
        IntervalEnding = DateTimeOffset.Parse(isoTime),
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
    public void Analyze_SplitsEnergyIntoCorrectTimeOfDayBands()
    {
        // One reading in each band; 4 kW over a 15-min slice = 1 kWh each.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00Z", 4m),  // morning
            Reading("2026-05-01T14:00:00Z", 4m),  // daytime
            Reading("2026-05-01T19:00:00Z", 4m),  // evening
            Reading("2026-05-01T02:00:00Z", 4m),  // night
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.MorningKwh.Should().Be(1m);
        facts.DayKwh.Should().Be(1m);
        facts.EveningKwh.Should().Be(1m);
        facts.NightKwh.Should().Be(1m);
    }

    [Fact]
    public void Analyze_FindsBusiestHour()
    {
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00Z", 1m),
            Reading("2026-05-01T19:00:00Z", 5m),  // heaviest
            Reading("2026-05-01T19:15:00Z", 4m),  // same hour, adds to it
            Reading("2026-05-01T22:00:00Z", 2m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.PeakHour.Should().Be(19);
    }

    [Fact]
    public void Analyze_CapturesHighestSingleDrawAndTimestamp()
    {
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00Z", 2m),
            Reading("2026-05-01T20:30:00Z", 7.5m),  // the spike
            Reading("2026-05-01T22:00:00Z", 3m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        facts.HighestDemandKW.Should().Be(7.5m);
        facts.HighestDemandOn.Should().Be(DateTimeOffset.Parse("2026-05-01T20:30:00Z"));
    }

    [Fact]
    public void Analyze_AveragesDailyUseAcrossDistinctDays()
    {
        // 4 kW slices = 1 kWh each; two readings per day over two days = 1 kWh/day.
        var readings = new List<TelemetryLedger>
        {
            Reading("2026-05-01T08:00:00Z", 2m),
            Reading("2026-05-01T20:00:00Z", 2m),
            Reading("2026-05-02T08:00:00Z", 2m),
            Reading("2026-05-02T20:00:00Z", 2m),
        };

        var facts = ConsumptionAnalyzer.Analyze(readings);

        // total = 4 slices * (2 kW * 0.25h) = 2 kWh over 2 days = 1 kWh/day.
        facts.AverageDailyKwh.Should().Be(1m);
    }
}
