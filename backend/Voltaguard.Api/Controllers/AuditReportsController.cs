using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Dtos;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit-reports")]
public class AuditReportsController : ControllerBase
{
    private readonly IAuditReportService _auditReportService;

    public AuditReportsController(IAuditReportService auditReportService)
    {
        _auditReportService = auditReportService;
    }

    /// <summary>Lists the signed-in user's audit reports, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AuditReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditReportDto>>> GetMine(CancellationToken cancellationToken)
    {
        var reports = await _auditReportService.GetMyReportsAsync(User.GetUserId(), cancellationToken);
        return Ok(reports);
    }

    /// <summary>Returns one report, including the AI findings once they are ready.</summary>
    [HttpGet("{auditReportId:int}")]
    [ProducesResponseType(typeof(AuditReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditReportDto>> GetById(int auditReportId, CancellationToken cancellationToken)
    {
        var report = await _auditReportService.GetByIdAsync(auditReportId, User.GetUserId(), cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }
}
