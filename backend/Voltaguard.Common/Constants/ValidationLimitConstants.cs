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

    // Only plain-text ledger exports are accepted. The extension is checked rather
    // than the content type, because Windows browsers often send a .csv as
    // application/vnd.ms-excel.
    public static readonly IReadOnlyList<string> AllowedFileExtensions = [".csv", ".txt"];

    // How much of the start of a file is scanned for a NUL byte. Text files never
    // contain one, so finding it means the upload is binary (a spreadsheet, image...).
    public const int BinaryCheckBytes = 8 * 1024;

    // Number of comma-separated columns each telemetry row must have.
    public const int ExpectedColumnCount = 8;

    // Physical limits: a reading outside these is corrupt data and is rejected.
    // The database CHECK constraints use the same values.
    public const decimal MinVoltage = 100m;
    public const decimal MaxVoltage = 300m;

    public const decimal MinFrequency = 47m;
    public const decimal MaxFrequency = 53m;

    public const decimal MinPowerFactor = 0m;
    public const decimal MaxPowerFactor = 1m;

    // Normal supply band: 230 V nominal +/- 6% (Indian supply-voltage norm for LT
    // consumers). A valid reading below it is a sag, above it a swell. The two
    // limits are written out so they read the same as on the charts.
    public const decimal NominalVoltage = 230m;
    public const decimal NormalMinVoltage = 216.2m;   // 230 V - 6%
    public const decimal NormalMaxVoltage = 243.8m;   // 230 V + 6%

    // On a single-phase supply the neutral carries the same current as the phase.
    // A gap larger than this means current is leaving by another path, a common
    // sign of meter tampering (neutral bypass) or earth leakage. 1 A sits well
    // above meter rounding noise yet catches a partial bypass at household loads.
    public const decimal CurrentMismatchThresholdA = 1.0m;
}
