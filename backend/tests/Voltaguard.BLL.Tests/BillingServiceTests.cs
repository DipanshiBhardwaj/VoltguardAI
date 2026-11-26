using FluentAssertions;
using Moq;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Enums;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;
using Xunit;

namespace Voltaguard.BLL.Tests;

public class BillingServiceTests
{
    private const int MeterId = 1;
    private const int OwnerId = 10;
    private static readonly TimeSpan Ist = new(5, 30, 0);

    private readonly Mock<IMeterRepository> _meterRepository = new();
    private readonly Mock<ITelemetryRepository> _telemetryRepository = new();
    private readonly Mock<ITariffRepository> _tariffRepository = new();
    private readonly Mock<IAuditReportRepository> _auditReportRepository = new();

    public BillingServiceTests()
    {
        _meterRepository
            .Setup(r => r.GetOwnedByIdAsync(MeterId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartMeterAsset { Id = MeterId, OwnerUserId = OwnerId, SlabCode = SlabCode.DomesticStandard });
        _telemetryRepository
            .Setup(r => r.GetByMeterAndPeriodAsync(MeterId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelemetryLedger>());
        _tariffRepository
            .Setup(r => r.GetBySlabCodeAsync(SlabCode.DomesticStandard, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TariffSlab>());
    }

    private BillingService CreateService() => new(
        _meterRepository.Object, _telemetryRepository.Object, _tariffRepository.Object, _auditReportRepository.Object);

    [Fact]
    public async Task RunAuditAsync_IstPeriod_QueriesWholeLastDayInUtc()
    {
        // 1 May 00:00 IST to the end of 31 May IST, as the dashboard sends it.
        var request = new RunAuditRequest
        {
            BillingCyclePeriod = "2026-05",
            FromDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, Ist),
            ToDate = new DateTimeOffset(2026, 5, 31, 23, 59, 59, 999, Ist)
        };

        await CreateService().RunAuditAsync(MeterId, OwnerId, request);

        // Both bounds reach the repository in UTC, and the end bound covers all of 31 May IST.
        _telemetryRepository.Verify(r => r.GetByMeterAndPeriodAsync(
            MeterId,
            It.Is<DateTimeOffset>(d => d.Offset == TimeSpan.Zero && d == new DateTimeOffset(2026, 4, 30, 18, 30, 0, TimeSpan.Zero)),
            It.Is<DateTimeOffset>(d => d.Offset == TimeSpan.Zero && d == new DateTimeOffset(2026, 5, 31, 18, 29, 59, 999, TimeSpan.Zero)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAuditAsync_EndBeforeStart_Throws()
    {
        var request = new RunAuditRequest
        {
            BillingCyclePeriod = "2026-05",
            FromDate = new DateTimeOffset(2026, 5, 31, 0, 0, 0, Ist),
            ToDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, Ist)
        };

        var act = () => CreateService().RunAuditAsync(MeterId, OwnerId, request);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
