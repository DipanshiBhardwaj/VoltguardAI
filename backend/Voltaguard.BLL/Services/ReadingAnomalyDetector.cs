using Voltaguard.Common.Constants;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;

namespace Voltaguard.BLL.Services;

/// <summary>
/// Classifies a valid reading as abnormal. Only readings that already passed the
/// parser's physical limits reach here, so a voltage outside the normal supply
/// band is a real supply event (sag or swell), not corrupt data. A gap between
/// phase and neutral current points to tampering or earth leakage. Pure and
/// stateless: the same reading always gives the same answer.
/// </summary>
public static class ReadingAnomalyDetector
{
    private static readonly IReadOnlyList<AnomalyType> None = Array.Empty<AnomalyType>();

    /// <summary>Every way the reading is abnormal; empty when it is normal.</summary>
    public static IReadOnlyList<AnomalyType> Detect(TelemetryLedger reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        List<AnomalyType>? found = null;

        if (reading.VoltageV < ValidationLimitConstants.NormalMinVoltage)
        {
            (found ??= new()).Add(AnomalyType.VoltageSag);
        }
        else if (reading.VoltageV > ValidationLimitConstants.NormalMaxVoltage)
        {
            (found ??= new()).Add(AnomalyType.VoltageSwell);
        }

        if (IsCurrentMismatch(reading))
        {
            (found ??= new()).Add(AnomalyType.CurrentMismatch);
        }

        return found ?? None;
    }

    public static bool IsCurrentMismatch(TelemetryLedger reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return Math.Abs(reading.PhaseCurrentA - reading.NeutralCurrentA) > ValidationLimitConstants.CurrentMismatchThresholdA;
    }
}
