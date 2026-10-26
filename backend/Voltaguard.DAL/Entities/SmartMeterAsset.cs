using Voltaguard.Common.Enums;

namespace Voltaguard.DAL.Entities;

/// <summary>
/// A consumer smart meter. Owned by exactly one user; all of its telemetry and
/// audit reports hang off this asset. The owner link is what keeps one user's
/// data isolated from another's.
/// </summary>
public class SmartMeterAsset : BaseEntity
{
    public string SerialNumber { get; set; } = string.Empty;

    public SlabCode SlabCode { get; set; }

    public int OwnerUserId { get; set; }
    public UserAccount? Owner { get; set; }

    public ICollection<TelemetryLedger> Readings { get; set; } = new List<TelemetryLedger>();
    public ICollection<EnergyAuditReport> AuditReports { get; set; } = new List<EnergyAuditReport>();
}
