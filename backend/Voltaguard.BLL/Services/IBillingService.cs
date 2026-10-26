using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;

namespace Voltaguard.BLL.Services;

public interface IBillingService
{
    /// <summary>
    /// Computes the deterministic bill for a meter over a period and files an audit
    /// report for the AI agent to annotate. Returns the per-slab breakdown.
    /// </summary>
    Task<BillSummaryDto> RunAuditAsync(
        int meterAssetId,
        int ownerUserId,
        RunAuditRequest request,
        CancellationToken cancellationToken = default);
}
