using FluentAssertions;
using Moq;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class MeterServiceTests
{
    private const int MeterId = 1;
    private const int OwnerId = 10;
    private const int OtherUserId = 99;
    private static readonly TimeSpan Ist = new(5, 30, 0);

    private readonly Mock<IMeterRepository> _meterRepository = new();
    private readonly Mock<ITelemetryRepository> _telemetryRepository = new();
    private readonly SmartMeterAsset _meter = new()
    {
        Id = MeterId,
        SerialNumber = "M-1",
        OwnerUserId = OwnerId,
        SlabCode = SlabCode.DomesticStandard
    };

    public MeterServiceTests()
    {
        // The meter belongs to OwnerId; any other user gets null, like the real repository.
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_meter);
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OtherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartMeterAsset?)null);

        _telemetryRepository
            .Setup(r => r.GetRecentByMeterAsync(MeterId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>());
        _telemetryRepository
            .Setup(r => r.GetAbnormalByMeterAsync(MeterId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>());
    }

    private MeterService CreateService() => new(_meterRepository.Object, _telemetryRepository.Object);

    private static TelemetryLedger Reading(long logId, int day, decimal voltage, decimal phase, decimal neutral) => new()
    {
        LogId = logId,
        MeterAssetId = MeterId,
        IntervalEnding = new DateTimeOffset(2026, 5, day, 12, 0, 0, TimeSpan.Zero),
        VoltageV = voltage,
        PhaseCurrentA = phase,
        NeutralCurrentA = neutral
    };

    [Fact]
    public async Task GetMyMetersAsync_MeterWithManyReadings_ReportsStoredCountNotLoadedCount()
    {
        _meterRepository
            .Setup(r => r.GetByOwnerAsync(OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmartMeterAsset> { _meter });

        var latest = new TelemetryLedger { MeterAssetId = MeterId, IntervalEnding = new DateTimeOffset(2026, 5, 31, 18, 15, 0, TimeSpan.Zero) };
        _telemetryRepository
            .Setup(r => r.GetRecentByMeterAsync(MeterId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger> { latest });
        _telemetryRepository
            .Setup(r => r.CountByMeterAsync(MeterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2976);

        var result = await CreateService().GetMyMetersAsync(OwnerId);

        result.Should().ContainSingle();
        result[0].ReadingCount.Should().Be(2976);
        result[0].LatestReadingOn.Should().Be(latest.IntervalEnding);
    }

    [Fact]
    public async Task GetMyMetersAsync_AbnormalReadingsStored_ReportsAnomalyCountPerType()
    {
        _meterRepository
            .Setup(r => r.GetByOwnerAsync(OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmartMeterAsset> { _meter });
        _telemetryRepository
            .Setup(r => r.GetAbnormalByMeterAsync(MeterId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>
            {
                Reading(1, 1, 205m, 5m, 5m),    // sag
                Reading(2, 2, 230m, 6m, 2m),    // mismatch
                Reading(3, 3, 204m, 6m, 2m),    // sag + mismatch
            });

        var result = await CreateService().GetMyMetersAsync(OwnerId);

        result[0].AnomalyCount.Should().Be(4);
    }

    [Fact]
    public async Task UpdateAsync_OwnMeterWithFreeSerial_ChangesSerialAndTariff()
    {
        var request = new UpdateMeterRequest { SerialNumber = "M-NEW", SlabCode = SlabCode.DomesticLifeline };

        var result = await CreateService().UpdateAsync(MeterId, OwnerId, request);

        result.SerialNumber.Should().Be("M-NEW");
        result.SlabCode.Should().Be(SlabCode.DomesticLifeline);
        _meter.SerialNumber.Should().Be("M-NEW");
        _meter.SlabCode.Should().Be(SlabCode.DomesticLifeline);
        _meterRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnSerial_ChecksOtherMetersOnly()
    {
        var request = new UpdateMeterRequest { SerialNumber = "M-1", SlabCode = SlabCode.DomesticLifeline };

        await CreateService().UpdateAsync(MeterId, OwnerId, request);

        _meterRepository.Verify(r => r.SerialExistsForOtherMeterAsync("M-1", MeterId, It.IsAny<CancellationToken>()), Times.Once);
        _meterRepository.Verify(r => r.SerialExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _meter.SlabCode.Should().Be(SlabCode.DomesticLifeline);
    }

    [Fact]
    public async Task UpdateAsync_SerialUsedByAnotherMeter_Throws()
    {
        _meterRepository
            .Setup(r => r.SerialExistsForOtherMeterAsync("TAKEN", MeterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new UpdateMeterRequest { SerialNumber = "TAKEN", SlabCode = SlabCode.DomesticStandard };

        var act = () => CreateService().UpdateAsync(MeterId, OwnerId, request);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*serial number already exists*");
        _meter.SerialNumber.Should().Be("M-1");
        _meterRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersMeter_Throws()
    {
        var request = new UpdateMeterRequest { SerialNumber = "HIJACK", SlabCode = SlabCode.DomesticLifeline };

        var act = () => CreateService().UpdateAsync(MeterId, OtherUserId, request);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Meter not found*");
        _meter.SerialNumber.Should().Be("M-1");
        _meterRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_OwnMeter_RemovesAndSaves()
    {
        await CreateService().DeleteAsync(MeterId, OwnerId);

        _meterRepository.Verify(r => r.Remove(_meter), Times.Once);
        _meterRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersMeter_ThrowsAndRemovesNothing()
    {
        var act = () => CreateService().DeleteAsync(MeterId, OtherUserId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Meter not found*");
        _meterRepository.Verify(r => r.Remove(It.IsAny<SmartMeterAsset>()), Times.Never);
        _meterRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAnomaliesAsync_MixedReadings_ReturnsOneEntryPerTypeNewestFirst()
    {
        // The repository hands back candidates newest first.
        _telemetryRepository
            .Setup(r => r.GetAbnormalByMeterAsync(MeterId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>
            {
                Reading(3, 20, 204m, 6m, 2m),   // sag + mismatch
                Reading(2, 12, 250m, 5m, 5m),   // swell
            });

        var result = await CreateService().GetAnomaliesAsync(MeterId, OwnerId, null, null);

        result.Select(a => (a.LogId, a.Type)).Should().Equal(
            (3L, AnomalyType.VoltageSag),
            (3L, AnomalyType.CurrentMismatch),
            (2L, AnomalyType.VoltageSwell));
        result[1].PhaseCurrentA.Should().Be(6m);
        result[1].NeutralCurrentA.Should().Be(2m);
    }

    [Fact]
    public async Task GetAnomaliesAsync_IstRange_QueriesRepositoryInUtc()
    {
        var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, Ist);
        var to = new DateTimeOffset(2026, 5, 31, 23, 59, 59, 999, Ist);

        await CreateService().GetAnomaliesAsync(MeterId, OwnerId, from, to);

        _telemetryRepository.Verify(r => r.GetAbnormalByMeterAsync(
            MeterId,
            It.Is<DateTimeOffset?>(d => d!.Value.Offset == TimeSpan.Zero && d.Value == from),
            It.Is<DateTimeOffset?>(d => d!.Value.Offset == TimeSpan.Zero && d.Value == to),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAnomaliesAsync_AnotherUsersMeter_Throws()
    {
        var act = () => CreateService().GetAnomaliesAsync(MeterId, OtherUserId, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetReadingsInRangeAsync_IstRange_QueriesRepositoryInUtcWithLimit()
    {
        var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, Ist);
        var to = new DateTimeOffset(2026, 5, 31, 23, 59, 59, 999, Ist);
        _telemetryRepository
            .Setup(r => r.GetRangeByMeterAsync(MeterId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), 3000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger> { Reading(1, 5, 230m, 5m, 5m) });

        var result = await CreateService().GetReadingsInRangeAsync(MeterId, OwnerId, from, to, 3000);

        result.Should().ContainSingle();
        _telemetryRepository.Verify(r => r.GetRangeByMeterAsync(
            MeterId,
            It.Is<DateTimeOffset?>(d => d!.Value.Offset == TimeSpan.Zero && d.Value == from),
            It.Is<DateTimeOffset?>(d => d!.Value.Offset == TimeSpan.Zero && d.Value == to),
            3000,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReadingsInRangeAsync_EndBeforeStart_Throws()
    {
        var act = () => CreateService().GetReadingsInRangeAsync(
            MeterId, OwnerId, new DateTimeOffset(2026, 5, 31, 0, 0, 0, Ist), new DateTimeOffset(2026, 5, 1, 0, 0, 0, Ist), 3000);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
