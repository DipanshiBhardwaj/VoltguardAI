using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meters/{meterAssetId:int}/audit")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>
    /// Runs the deterministic billing audit for a meter over a period and files a
    /// report for the AI agent to annotate. Returns the per-slab cost breakdown.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BillSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BillSummaryDto>> RunAudit(
        int meterAssetId,
        [FromBody] RunAuditRequest request,
        CancellationToken cancellationToken)
    {
        var summary = await _billingService.RunAuditAsync(meterAssetId, User.GetUserId(), request, cancellationToken);
        return Ok(summary);
    }
}
