using System.Globalization;
using Voltaguard.Common.Constants;
using Voltaguard.DAL.Entities;

namespace Voltaguard.BLL.Services;

/// <summary>
/// Validates and converts one line of an uploaded meter file. Runs the tokenize →
/// cast → physics-bounds stages from the ingestion pipeline. Boundary checks on the
/// whole file (size, type) are handled by the caller before any line reaches here.
/// </summary>
public static class TelemetryRowParser
{
    public static RowParseOutcome Parse(string line, int meterAssetId)
    {
        // Expected columns, in order:
        // IntervalEnding, VoltageV, ActivePowerKW, PhaseCurrentA,
        // NeutralCurrentA, PowerFactor, FrequencyHz, CumulativeKWH
        var cells = line.Split(',');

        if (cells.Length != ValidationLimitConstants.ExpectedColumnCount)
        {
            return RowParseOutcome.Rejected(
                $"Expected {ValidationLimitConstants.ExpectedColumnCount} columns but found {cells.Length}.");
        }

        if (!TryParseTimestamp(cells[0], out var intervalEnding))
        {
            return RowParseOutcome.Rejected("Column 1 is not a valid timestamp.");
        }

        if (!TryParseDecimal(cells[1], out var voltage) ||
            !TryParseDecimal(cells[2], out var activePower) ||
            !TryParseDecimal(cells[3], out var phaseCurrent) ||
            !TryParseDecimal(cells[4], out var neutralCurrent) ||
            !TryParseDecimal(cells[5], out var powerFactor) ||
            !TryParseDecimal(cells[6], out var frequency) ||
            !TryParseDecimal(cells[7], out var cumulativeKwh))
        {
            return RowParseOutcome.Rejected("One or more numeric columns could not be read.");
        }

        var physicsError = CheckPhysics(voltage, frequency, powerFactor);
        if (physicsError is not null)
        {
            return RowParseOutcome.Rejected(physicsError);
        }

        var reading = new TelemetryLedger
        {
            MeterAssetId = meterAssetId,
            IntervalEnding = intervalEnding,
            VoltageV = voltage,
            ActivePowerKW = activePower,
            PhaseCurrentA = phaseCurrent,
            NeutralCurrentA = neutralCurrent,
            PowerFactor = powerFactor,
            FrequencyHz = frequency,
            CumulativeKWH = cumulativeKwh
        };

        return RowParseOutcome.Accepted(reading);
    }

    // A reading outside these physical ranges is corrupt data, not a real measurement.
    private static string? CheckPhysics(decimal voltage, decimal frequency, decimal powerFactor)
    {
        if (voltage < ValidationLimitConstants.MinVoltage || voltage > ValidationLimitConstants.MaxVoltage)
        {
            return $"Voltage {voltage} is outside the plausible {ValidationLimitConstants.MinVoltage}-{ValidationLimitConstants.MaxVoltage} V range.";
        }

        if (frequency < ValidationLimitConstants.MinFrequency || frequency > ValidationLimitConstants.MaxFrequency)
        {
            return $"Frequency {frequency} is outside the plausible {ValidationLimitConstants.MinFrequency}-{ValidationLimitConstants.MaxFrequency} Hz range.";
        }

        if (powerFactor < ValidationLimitConstants.MinPowerFactor || powerFactor > ValidationLimitConstants.MaxPowerFactor)
        {
            return $"Power factor {powerFactor} is outside the valid 0-1 range.";
        }

        return null;
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(
            value.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out result);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}

/// <summary>Result of parsing one line: either a reading to save or a rejection reason.</summary>
public sealed class RowParseOutcome
{
    private RowParseOutcome(bool isAccepted, TelemetryLedger? reading, string? reason)
    {
        IsAccepted = isAccepted;
        Reading = reading;
        Reason = reason;
    }

    public bool IsAccepted { get; }
    public TelemetryLedger? Reading { get; }
    public string? Reason { get; }

    public static RowParseOutcome Accepted(TelemetryLedger reading) => new(true, reading, null);
    public static RowParseOutcome Rejected(string reason) => new(false, null, reason);
}
