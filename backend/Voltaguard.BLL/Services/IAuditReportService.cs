using Voltaguard.Common.Dtos;

namespace Voltaguard.BLL.Services;

public interface IAuditReportService
{
    Task<List<AuditReportDto>> GetMyReportsAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<AuditReportDto?> GetByIdAsync(int auditReportId, int ownerUserId, CancellationToken cancellationToken = default);
}
