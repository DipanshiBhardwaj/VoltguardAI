using Voltaguard.Common.Enums;

namespace Voltaguard.Common.Dtos;

/// <summary>
/// A stored audit report. <see cref="AiAnalysisFindings"/> is null until the local
/// AI agent has processed the report and written its plain-English explanation.
/// </summary>
public sealed class AuditReportDto
{
    public int AuditReportId { get; init; }
    public int MeterAssetId { get; init; }
    public string BillingCyclePeriod { get; init; } = string.Empty;
    public decimal CalculatedDeterministicCost { get; init; }
    public decimal TotalUnits { get; init; }
    public ConsumptionFactsDto Facts { get; init; } = new();
    public string? AiAnalysisFindings { get; init; }
    public AuditReportStatus Status { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
}
