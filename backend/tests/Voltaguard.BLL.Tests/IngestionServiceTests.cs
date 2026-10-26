using System.Text;
using FluentAssertions;
using Moq;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class IngestionServiceTests
{
    private const int MeterId = 1;
    private const int OwnerId = 10;

    private readonly Mock<IMeterRepository> _meterRepository = new();
    private readonly Mock<ITelemetryRepository> _telemetryRepository = new();

    private IngestionService CreateService() =>
        new(_meterRepository.Object, _telemetryRepository.Object);

    private void MeterIsOwned()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartMeterAsset { Id = MeterId, OwnerUserId = OwnerId, SlabCode = SlabCode.DomesticStandard });
    }

    private static Stream StreamOf(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task IngestAsync_AllValidRows_SavesEveryRow()
    {
        MeterIsOwned();
        var csv =
            "IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n" +
            "2026-05-01T18:15:00Z,230.5,2.10,9.50,0.20,0.95,50.00,1234.75\n" +
            "2026-05-01T18:30:00Z,229.0,1.80,8.10,0.15,0.96,49.98,1235.20\n";
        var stream = StreamOf(csv);

        var result = await CreateService().IngestAsync(MeterId, OwnerId, stream, stream.Length);

        result.AcceptedRows.Should().Be(2);
        result.RejectedRows.Should().Be(0);
        _telemetryRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<TelemetryLedger>>(rows => rows.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_MixedFile_SavesGoodRowsAndReportsBadOnes()
    {
        MeterIsOwned();
        var csv =
            "IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n" +
            "2026-05-01T18:15:00Z,230.5,2.10,9.50,0.20,0.95,50.00,1234.75\n" +
            "broken,line\n" +
            "2026-05-01T18:30:00Z,999,1.80,8.10,0.15,0.96,50.00,1235.20\n";
        var stream = StreamOf(csv);

        var result = await CreateService().IngestAsync(MeterId, OwnerId, stream, stream.Length);

        result.AcceptedRows.Should().Be(1);
        result.RejectedRows.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task IngestAsync_MeterNotOwnedByCaller_Throws()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartMeterAsset?)null);
        var stream = StreamOf("whatever");

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, stream, stream.Length);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IngestAsync_FileOverSizeLimit_Throws()
    {
        var stream = StreamOf("x");
        var oversized = (10L * 1024 * 1024) + 1;

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, stream, oversized);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
