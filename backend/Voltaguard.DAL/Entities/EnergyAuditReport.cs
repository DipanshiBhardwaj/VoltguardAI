using Voltaguard.Common.Enums;

namespace Voltaguard.DAL.Entities;

/// <summary>
/// A billing audit for one meter over one period. The deterministic cost is
/// computed and stored in C#; the plain-English findings are filled in later by
/// the local AI agent, which polls for reports still in the Pending state.
/// </summary>
public class EnergyAuditReport : BaseEntity
{
    public int MeterAssetId { get; set; }
    public SmartMeterAsset? Meter { get; set; }

    public string BillingCyclePeriod { get; set; } = string.Empty;

    public decimal CalculatedDeterministicCost { get; set; }

    public decimal TotalUnits { get; set; }

    // Measured consumption facts, computed deterministically from the readings and
    // used to ground the AI explanation rather than let it guess.
    public int PeakHour { get; set; }
    public decimal MorningKwh { get; set; }
    public decimal DayKwh { get; set; }
    public decimal EveningKwh { get; set; }
    public decimal NightKwh { get; set; }
    public decimal HighestDemandKW { get; set; }
    public DateTimeOffset? HighestDemandOn { get; set; }
    public decimal AverageDailyKwh { get; set; }

    public string? AiAnalysisFindings { get; set; }

    public AuditReportStatus Status { get; set; } = AuditReportStatus.Pending;
}
