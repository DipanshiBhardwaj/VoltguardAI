using System.ComponentModel.DataAnnotations;

namespace Voltaguard.Common.Requests;

/// <summary>
/// Asks for a billing audit of one meter over a date range. The period label
/// (e.g. "2026-05") is what the report is filed under.
/// </summary>
public sealed class RunAuditRequest
{
    [Required]
    [MaxLength(50)]
    public string BillingCyclePeriod { get; init; } = string.Empty;

    [Required]
    public DateTimeOffset FromDate { get; init; }

    [Required]
    public DateTimeOffset ToDate { get; init; }
}
