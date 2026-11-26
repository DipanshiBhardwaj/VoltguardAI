using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voltaguard.DAL;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.DAL.Tests;

public class TelemetryRepositoryTests
{
    private static VoltaguardDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<VoltaguardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VoltaguardDbContext(options);
    }

    private static TelemetryLedger Reading(int meterId, int minute) => new()
    {
        MeterAssetId = meterId,
        IntervalEnding = new DateTimeOffset(2026, 5, 1, 0, minute, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task CountByMeterAsync_ReadingsForSeveralMeters_CountsOnlyThatMeter()
    {
        await using var db = NewContext();
        db.TelemetryLedger.AddRange(Reading(1, 0), Reading(1, 15), Reading(1, 30), Reading(2, 0));
        await db.SaveChangesAsync();

        var count = await new TelemetryRepository(db).CountByMeterAsync(1);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetRangeByMeterAsync_RangeAndLimit_ReturnsNewestReadingsInRange()
    {
        await using var db = NewContext();
        db.TelemetryLedger.AddRange(Reading(1, 0), Reading(1, 15), Reading(1, 30), Reading(1, 45), Reading(2, 30));
        await db.SaveChangesAsync();

        var result = await new TelemetryRepository(db).GetRangeByMeterAsync(
            1,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 0, 30, 0, TimeSpan.Zero),
            take: 2);

        result.Select(r => r.IntervalEnding.Minute).Should().Equal(30, 15);
    }

    [Fact]
    public async Task GetRangeByMeterAsync_OpenEndedRange_ReturnsFromStartOnwards()
    {
        await using var db = NewContext();
        db.TelemetryLedger.AddRange(Reading(1, 0), Reading(1, 15), Reading(1, 30));
        await db.SaveChangesAsync();

        var result = await new TelemetryRepository(db).GetRangeByMeterAsync(
            1, new DateTimeOffset(2026, 5, 1, 0, 15, 0, TimeSpan.Zero), null, take: 100);

        result.Select(r => r.IntervalEnding.Minute).Should().Equal(30, 15);
    }

    [Fact]
    public async Task GetAbnormalByMeterAsync_MixedReadings_ReturnsOnlyOutOfBandOrMismatchedNewestFirst()
    {
        await using var db = NewContext();
        TelemetryLedger At(int minute, decimal volts, decimal phase, decimal neutral, int meterId = 1)
        {
            var r = Reading(meterId, minute);
            r.VoltageV = volts;
            r.PhaseCurrentA = phase;
            r.NeutralCurrentA = neutral;
            return r;
        }
        db.TelemetryLedger.AddRange(
            At(0, 230m, 5m, 5m),          // normal
            At(5, 216.2m, 5m, 5m),        // on the band edge: normal
            At(10, 205m, 5m, 5m),         // sag
            At(15, 250m, 5m, 5m),         // swell
            At(20, 230m, 5m, 4m),         // gap exactly 1 A: normal
            At(25, 230m, 3.63m, 1.09m),   // mismatch
            At(30, 205m, 5m, 5m, meterId: 2));
        await db.SaveChangesAsync();

        var result = await new TelemetryRepository(db).GetAbnormalByMeterAsync(1, null, null);

        result.Select(r => r.IntervalEnding.Minute).Should().Equal(25, 15, 10);
    }

    [Fact]
    public async Task GetExistingIntervalsAsync_RangeGiven_ReturnsOnlyThatMetersIntervalsInRange()
    {
        await using var db = NewContext();
        db.TelemetryLedger.AddRange(Reading(1, 0), Reading(1, 15), Reading(1, 30), Reading(1, 45), Reading(2, 15));
        await db.SaveChangesAsync();

        var result = await new TelemetryRepository(db).GetExistingIntervalsAsync(
            1,
            new DateTimeOffset(2026, 5, 1, 0, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 0, 30, 0, TimeSpan.Zero));

        result.Should().BeEquivalentTo(new[]
        {
            new DateTimeOffset(2026, 5, 1, 0, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 0, 30, 0, TimeSpan.Zero)
        });
    }
}
