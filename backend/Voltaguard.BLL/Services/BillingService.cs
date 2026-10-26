using Voltaguard.Common.Dtos;
using Voltaguard.Common.Enums;
using Voltaguard.Common.Requests;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class BillingService : IBillingService
{
    private readonly IMeterRepository _meterRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ITariffRepository _tariffRepository;
    private readonly IAuditReportRepository _auditReportRepository;

    public BillingService(
        IMeterRepository meterRepository,
        ITelemetryRepository telemetryRepository,
        ITariffRepository tariffRepository,
        IAuditReportRepository auditReportRepository)
    {
        _meterRepository = meterRepository;
        _telemetryRepository = telemetryRepository;
        _tariffRepository = tariffRepository;
        _auditReportRepository = auditReportRepository;
    }

    public async Task<BillSummaryDto> RunAuditAsync(
        int meterAssetId,
        int ownerUserId,
        RunAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ToDate < request.FromDate)
        {
            throw new InvalidOperationException("The end date cannot be before the start date.");
        }

        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("Meter not found for the current user.");
        }

        var readings = await _telemetryRepository.GetByMeterAndPeriodAsync(
            meterAssetId, request.FromDate, request.ToDate, cancellationToken);

        var totalUnits = CalculateUnits(readings);

        var slabs = await _tariffRepository.GetBySlabCodeAsync(meter.SlabCode, cancellationToken);
        var breakdown = TariffCalculator.Calculate(totalUnits, slabs);

        var facts = ConsumptionAnalyzer.Analyze(readings);

        var report = new EnergyAuditReport
        {
            MeterAssetId = meterAssetId,
            BillingCyclePeriod = request.BillingCyclePeriod,
            TotalUnits = totalUnits,
            CalculatedDeterministicCost = breakdown.TotalCost,
            PeakHour = facts.PeakHour,
            MorningKwh = facts.MorningKwh,
            DayKwh = facts.DayKwh,
            EveningKwh = facts.EveningKwh,
            NightKwh = facts.NightKwh,
            HighestDemandKW = facts.HighestDemandKW,
            HighestDemandOn = facts.HighestDemandOn,
            AverageDailyKwh = facts.AverageDailyKwh,
            Status = AuditReportStatus.Pending
        };

        await _auditReportRepository.AddAsync(report, cancellationToken);
        await _auditReportRepository.SaveChangesAsync(cancellationToken);

        return new BillSummaryDto
        {
            AuditReportId = report.Id,
            BillingCyclePeriod = request.BillingCyclePeriod,
            TotalUnits = totalUnits,
            TotalCost = breakdown.TotalCost,
            SlabCharges = breakdown.SlabCharges,
            Facts = ToFactsDto(facts)
        };
    }

    private static ConsumptionFactsDto ToFactsDto(ConsumptionFacts facts) => new()
    {
        PeakHour = facts.PeakHour,
        MorningKwh = facts.MorningKwh,
        DayKwh = facts.DayKwh,
        EveningKwh = facts.EveningKwh,
        NightKwh = facts.NightKwh,
        HighestDemandKW = facts.HighestDemandKW,
        HighestDemandOn = facts.HighestDemandOn,
        AverageDailyKwh = facts.AverageDailyKwh
    };

    // Consumption is the difference between the first and last odometer readings in
    // the window. A single reading cannot establish a delta, so it bills zero units.
    private static decimal CalculateUnits(IReadOnlyList<TelemetryLedger> readings)
    {
        if (readings.Count < 2)
        {
            return 0m;
        }

        var units = readings[^1].CumulativeKWH - readings[0].CumulativeKWH;
        return units > 0m ? decimal.Round(units, 2) : 0m;
    }
}
