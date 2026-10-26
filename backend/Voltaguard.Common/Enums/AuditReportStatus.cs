namespace Voltaguard.Common.Enums;

/// <summary>
/// Lifecycle of an energy audit report. The deterministic cost is written first
/// (Pending), then the local AI agent fills in the plain-English findings and
/// moves it to Completed.
/// </summary>
public enum AuditReportStatus
{
    Pending = 5,
    Completed = 10
}
