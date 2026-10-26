using Voltaguard.Common.Dtos;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class AuditReportService : IAuditReportService
{
    private readonly IAuditReportRepository _auditReportRepository;

    public AuditReportService(IAuditReportRepository auditReportRepository)
    {
        _auditReportRepository = auditReportRepository;
    }

    public async Task<List<AuditReportDto>> GetMyReportsAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var reports = await _auditReportRepository.GetByOwnerAsync(ownerUserId, cancellationToken);
        return reports.Select(ToDto).ToList();
    }

    public async Task<AuditReportDto?> GetByIdAsync(int auditReportId, int ownerUserId, CancellationToken cancellationToken = default)
    {
        var report = await _auditReportRepository.GetOwnedByIdAsync(auditReportId, ownerUserId, cancellationToken);
        return report is null ? null : ToDto(report);
    }

    private static AuditReportDto ToDto(EnergyAuditReport report)
    {
        return new AuditReportDto
        {
            AuditReportId = report.Id,
            MeterAssetId = report.MeterAssetId,
            BillingCyclePeriod = report.BillingCyclePeriod,
            CalculatedDeterministicCost = report.CalculatedDeterministicCost,
            TotalUnits = report.TotalUnits,
            Facts = new ConsumptionFactsDto
            {
                PeakHour = report.PeakHour,
                MorningKwh = report.MorningKwh,
                DayKwh = report.DayKwh,
                EveningKwh = report.EveningKwh,
                NightKwh = report.NightKwh,
                HighestDemandKW = report.HighestDemandKW,
                HighestDemandOn = report.HighestDemandOn,
                AverageDailyKwh = report.AverageDailyKwh
            },
            AiAnalysisFindings = report.AiAnalysisFindings,
            Status = report.Status,
            CreatedOn = report.CreatedOn
        };
    }
}
