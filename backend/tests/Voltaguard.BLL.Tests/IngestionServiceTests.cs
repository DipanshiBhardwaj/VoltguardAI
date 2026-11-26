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

    public IngestionServiceTests()
    {
        // By default nothing is stored yet for the meter.
        _telemetryRepository
            .Setup(r => r.GetExistingIntervalsAsync(MeterId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateTimeOffset>());
    }

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

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

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

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        result.AcceptedRows.Should().Be(1);
        result.RejectedRows.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task IngestAsync_IntervalAlreadyStored_RejectsRowAsDuplicate()
    {
        MeterIsOwned();
        _telemetryRepository
            .Setup(r => r.GetExistingIntervalsAsync(MeterId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateTimeOffset> { new(2026, 5, 1, 18, 15, 0, TimeSpan.Zero) });
        var csv =
            "IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n" +
            "2026-05-01T18:15:00Z,230.5,2.10,9.50,0.20,0.95,50.00,1234.75\n" +
            "2026-05-01T18:30:00Z,229.0,1.80,8.10,0.15,0.96,49.98,1235.20\n";
        var stream = StreamOf(csv);

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        result.AcceptedRows.Should().Be(1);
        result.RejectedRows.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { LineNumber = 2, Reason = "Duplicate reading for this time interval." });
        _telemetryRepository.Verify(r => r.GetExistingIntervalsAsync(
            MeterId,
            new DateTimeOffset(2026, 5, 1, 18, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 18, 30, 0, TimeSpan.Zero),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_IntervalRepeatedInSameFile_RejectsLaterRowAsDuplicate()
    {
        MeterIsOwned();
        // Lines 2 and 4 are the same instant: 18:15 UTC written once as UTC and
        // once as IST (23:45 +05:30), so the check compares instants, not text.
        var csv =
            "IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n" +
            "2026-05-01T18:15:00Z,230.5,2.10,9.50,0.20,0.95,50.00,1234.75\n" +
            "2026-05-01T18:30:00Z,229.0,1.80,8.10,0.15,0.96,49.98,1235.20\n" +
            "2026-05-01 23:45,230.5,2.10,9.50,0.20,0.95,50.00,1234.75\n";
        var stream = StreamOf(csv);

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        result.AcceptedRows.Should().Be(2);
        result.RejectedRows.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { LineNumber = 4, Reason = "Duplicate reading for this time interval." });
        _telemetryRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<TelemetryLedger>>(rows => rows.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_MeterNotOwnedByCaller_Throws()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartMeterAsset?)null);
        var stream = StreamOf("whatever");

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IngestAsync_FileOverSizeLimit_Throws()
    {
        var stream = StreamOf("x");
        var oversized = (10L * 1024 * 1024) + 1;

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, oversized);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private const string Header =
        "IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n";

    [Fact]
    public async Task IngestAsync_AbnormalReadings_CountsEachTypeAndFlagsOnlyMismatchAsTamper()
    {
        MeterIsOwned();
        List<TelemetryLedger>? saved = null;
        _telemetryRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TelemetryLedger>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TelemetryLedger>, CancellationToken>((rows, _) => saved = rows.ToList());
        var csv = Header +
            "2026-05-01 18:00,230.5,1.20,5.40,5.39,0.96,50.00,1000.00\n" +   // normal
            "2026-05-01 18:15,205.0,1.20,5.90,5.91,0.96,50.00,1000.30\n" +   // sag
            "2026-05-01 18:30,250.0,1.20,4.90,4.89,0.96,50.00,1000.60\n" +   // swell
            "2026-05-01 18:45,230.0,0.80,3.63,1.09,0.96,50.00,1000.80\n" +   // mismatch
            "2026-05-01 19:00,204.0,1.20,6.10,2.00,0.96,50.00,1001.10\n";    // sag + mismatch
        var stream = StreamOf(csv);

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        result.AcceptedRows.Should().Be(5);
        result.VoltageSagCount.Should().Be(2);
        result.VoltageSwellCount.Should().Be(1);
        result.CurrentMismatchCount.Should().Be(2);
        result.AnomaliesDetected.Should().Be(5);
        saved.Should().NotBeNull();
        saved!.Select(r => r.TamperFlag).Should().Equal(false, false, false, true, true);
    }

    [Fact]
    public async Task IngestAsync_NoAbnormalReadings_ReportsZeroAnomalies()
    {
        MeterIsOwned();
        var stream = StreamOf(Header + "2026-05-01 18:00,230.5,1.20,5.40,5.39,0.96,50.00,1000.00\n");

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        result.AnomaliesDetected.Should().Be(0);
    }

    [Theory]
    [InlineData("ledger.xlsx")]
    [InlineData("ledger.pdf")]
    [InlineData("ledger.csv.exe")]
    [InlineData("ledger")]
    public async Task IngestAsync_FileNotCsvOrTxt_ThrowsBeforeReadingIt(string fileName)
    {
        MeterIsOwned();
        var stream = StreamOf(Header + "2026-05-01 18:00,230.5,1.20,5.40,5.39,0.96,50.00,1000.00\n");

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, fileName, stream, stream.Length);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(IngestionService.UnsupportedFileTypeReason);
        _telemetryRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<TelemetryLedger>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("ledger.CSV")]
    [InlineData("ledger.txt")]
    public async Task IngestAsync_CsvOrTxtInAnyCase_IsAccepted(string fileName)
    {
        MeterIsOwned();
        var stream = StreamOf(Header + "2026-05-01 18:00,230.5,1.20,5.40,5.39,0.96,50.00,1000.00\n");

        var result = await CreateService().IngestAsync(MeterId, OwnerId, fileName, stream, stream.Length);

        result.AcceptedRows.Should().Be(1);
    }

    [Fact]
    public async Task IngestAsync_FileStartsWithNulByte_ThrowsAsBinary()
    {
        MeterIsOwned();
        // An .xlsx renamed to .csv: starts with a ZIP header, which contains NUL bytes.
        var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
        var stream = new MemoryStream(bytes);

        var act = () => CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, stream.Length);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(IngestionService.BinaryFileReason);
    }

    [Fact]
    public async Task IngestAsync_ForwardOnlyStream_StillReadsEveryRow()
    {
        MeterIsOwned();
        var csv = Header +
            "2026-05-01 18:00,230.5,1.20,5.40,5.39,0.96,50.00,1000.00\n" +
            "2026-05-01 18:15,230.5,1.20,5.40,5.39,0.96,50.00,1000.30\n";
        var stream = new ForwardOnlyStream(Encoding.UTF8.GetBytes(csv));

        var result = await CreateService().IngestAsync(MeterId, OwnerId, "ledger.csv", stream, csv.Length);

        result.AcceptedRows.Should().Be(2);
    }

    // Behaves like a network upload stream: readable once, no seeking.
    private sealed class ForwardOnlyStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}
