namespace Voltaguard.Common.Constants;

/// <summary>
/// Bounds the ingestion parser enforces on an uploaded meter file. The physical
/// ranges follow Indian single-phase distribution norms (230V nominal, 50Hz grid)
/// so a row that is outside them is almost certainly a corrupt or mistyped line
/// rather than a real reading.
/// </summary>
public static class ValidationLimitConstants
{
    // A domestic ledger export is small; anything larger is likely the wrong file.
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    // Number of comma-separated columns each telemetry row must have.
    public const int ExpectedColumnCount = 8;

    public const decimal MinVoltage = 100m;
    public const decimal MaxVoltage = 300m;

    public const decimal MinFrequency = 47m;
    public const decimal MaxFrequency = 53m;

    public const decimal MinPowerFactor = 0m;
    public const decimal MaxPowerFactor = 1m;
}
