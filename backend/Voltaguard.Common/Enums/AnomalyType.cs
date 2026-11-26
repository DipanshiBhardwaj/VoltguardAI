namespace Voltaguard.Common.Enums;

/// <summary>
/// Kind of abnormal (but physically valid) reading found in a meter's data.
/// Worked out from the reading on demand, never stored. Values are spaced so one
/// can be inserted later if needed.
/// </summary>
public enum AnomalyType
{
    VoltageSag = 5,
    VoltageSwell = 10,
    CurrentMismatch = 15
}
