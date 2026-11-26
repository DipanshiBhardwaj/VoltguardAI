using FluentAssertions;
using Moq;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class ForecastServiceTests
{
    private const int MeterId = 1;
    private const int OwnerId = 10;

    private readonly Mock<IMeterRepository> _meterRepository = new();
    private readonly Mock<ITelemetryRepository> _telemetryRepository = new();
    private readonly Mock<ITariffRepository> _tariffRepository = new();

    public ForecastServiceTests()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartMeterAsset { Id = MeterId, OwnerUserId = OwnerId, SlabCode = SlabCode.DomesticStandard });

        // The seeded Domestic Standard slabs.
        _tariffRepository
            .Setup(r => r.GetBySlabCodeAsync(SlabCode.DomesticStandard, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TariffSlab>
            {
                new() { SlabCode = SlabCode.DomesticStandard, FromUnit = 0, ToUnit = 125, RatePerUnit = 4.00m, ToDMultiplier = 1.0m, FixedCharge = 70 },
                new() { SlabCode = SlabCode.DomesticStandard, FromUnit = 125, ToUnit = 300, RatePerUnit = 5.25m, ToDMultiplier = 1.0m, FixedCharge = 0 },
                new() { SlabCode = SlabCode.DomesticStandard, FromUnit = 300, ToUnit = null, RatePerUnit = 6.00m, ToDMultiplier = 1.15m, FixedCharge = 0 }
            });
    }

    private ForecastService CreateService() =>
        new(_meterRepository.Object, _telemetryRepository.Object, _tariffRepository.Object);

    private static TelemetryLedger Reading(DateTimeOffset at, decimal cumulativeKwh) => new()
    {
        MeterAssetId = MeterId,
        IntervalEnding = at,
        CumulativeKWH = cumulativeKwh
    };

    // Latest reading at `latest`, plus the window the service will ask for.
    private void ReadingsAre(DateTimeOffset latest, params TelemetryLedger[] window)
    {
        _telemetryRepository
            .Setup(r => r.GetRecentByMeterAsync(MeterId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger> { Reading(latest, window.Length > 0 ? window[^1].CumulativeKWH : 0m) });
        _telemetryRepository
            .Setup(r => r.GetByMeterAndPeriodAsync(MeterId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(window.ToList());
    }

    [Fact]
    public async Task GetForecastAsync_ThirtyDaysAtTenUnitsADay_ProjectsNextMonthAndPricesIt()
    {
        // 31 May 23:45 IST; 300 units over exactly 30 days = 10 units/day.
        var latest = new DateTimeOffset(2026, 5, 31, 18, 15, 0, TimeSpan.Zero);
        ReadingsAre(latest,
            Reading(latest.AddDays(-30), 1000m),
            Reading(latest.AddDays(-15), 1150m),
            Reading(latest, 1300m));

        var result = await CreateService().GetForecastAsync(MeterId, OwnerId);

        // June has 30 days: 300 units = 125 @ 4.00 + 70 fixed + 175 @ 5.25 = 570 + 918.75.
        result.ProjectedMonth.Should().Be("2026-06");
        result.DaysInProjectedMonth.Should().Be(30);
        result.AverageDailyKwh.Should().Be(10m);
        result.ProjectedUnits.Should().Be(300m);
        result.ProjectedCost.Should().Be(1488.75m);
        result.SlabCharges.Should().HaveCount(2);
        result.BasedOnFrom.Should().Be(latest.AddDays(-30));
        result.BasedOnTo.Should().Be(latest);
    }

    [Fact]
    public async Task GetForecastAsync_LatestReading_QueriesTheLastThirtyDaysInUtc()
    {
        var latest = new DateTimeOffset(2026, 5, 31, 18, 15, 0, TimeSpan.Zero);
        ReadingsAre(latest, Reading(latest.AddDays(-30), 1000m), Reading(latest, 1300m));

        await CreateService().GetForecastAsync(MeterId, OwnerId);

        _telemetryRepository.Verify(r => r.GetByMeterAndPeriodAsync(
            MeterId,
            It.Is<DateTimeOffset>(d => d.Offset == TimeSpan.Zero && d == latest.AddDays(-30)),
            It.Is<DateTimeOffset>(d => d.Offset == TimeSpan.Zero && d == latest),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetForecastAsync_ShortHistory_AveragesOverTheDaysActuallyCovered()
    {
        // Only 10 days of data with 100 units: still 10 units/day, not 100/30.
        var latest = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        ReadingsAre(latest, Reading(latest.AddDays(-10), 1000m), Reading(latest, 1100m));

        var result = await CreateService().GetForecastAsync(MeterId, OwnerId);

        result.AverageDailyKwh.Should().Be(10m);
        result.ProjectedUnits.Should().Be(300m);   // June, 30 days
    }

    [Fact]
    public async Task GetForecastAsync_LatestReadingAfterIstMidnight_ProjectsMonthAfterTheIstDate()
    {
        // 31 May 20:00 UTC is already 1 June 01:30 IST, so the next month is July (31 days).
        var latest = new DateTimeOffset(2026, 5, 31, 20, 0, 0, TimeSpan.Zero);
        ReadingsAre(latest, Reading(latest.AddDays(-30), 1000m), Reading(latest, 1300m));

        var result = await CreateService().GetForecastAsync(MeterId, OwnerId);

        result.ProjectedMonth.Should().Be("2026-07");
        result.DaysInProjectedMonth.Should().Be(31);
        result.ProjectedUnits.Should().Be(310m);
    }

    [Fact]
    public async Task GetForecastAsync_LatestReadingInDecember_ProjectsJanuaryOfNextYear()
    {
        var latest = new DateTimeOffset(2026, 12, 15, 6, 0, 0, TimeSpan.Zero);
        ReadingsAre(latest, Reading(latest.AddDays(-30), 1000m), Reading(latest, 1300m));

        var result = await CreateService().GetForecastAsync(MeterId, OwnerId);

        result.ProjectedMonth.Should().Be("2027-01");
        result.DaysInProjectedMonth.Should().Be(31);
    }

    [Fact]
    public async Task GetForecastAsync_NoReadings_Throws()
    {
        _telemetryRepository
            .Setup(r => r.GetRecentByMeterAsync(MeterId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>());

        var act = () => CreateService().GetForecastAsync(MeterId, OwnerId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no readings*");
    }

    [Fact]
    public async Task GetForecastAsync_LessThanOneDayOfReadings_Throws()
    {
        var latest = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        ReadingsAre(latest, Reading(latest.AddHours(-6), 1000m), Reading(latest, 1003m));

        var act = () => CreateService().GetForecastAsync(MeterId, OwnerId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*full day*");
    }

    [Fact]
    public async Task GetForecastAsync_AnotherUsersMeter_Throws()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartMeterAsset?)null);

        var act = () => CreateService().GetForecastAsync(MeterId, 99);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Meter not found*");
        _telemetryRepository.Verify(r => r.GetRecentByMeterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
