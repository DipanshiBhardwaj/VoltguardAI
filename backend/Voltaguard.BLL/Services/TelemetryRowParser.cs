using System.Globalization;
using System.Text.RegularExpressions;
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
    public const string TimestampFormatReason = "Timestamp must be in YYYY-MM-DD HH:mm format.";
    public const string InvalidTimestampReason = "Column 1 is not a valid date and time.";

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

        var timestampError = TryParseTimestamp(cells[0], out var intervalEnding);
        if (timestampError is not null)
        {
            return RowParseOutcome.Rejected(timestampError);
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

    // Only ISO dates are accepted: a general date parser would silently read
    // 01/05/2026 as 5 January. Format: YYYY-MM-DD, a space or "T", HH:mm or
    // HH:mm:ss, then optionally "Z" or an offset such as +05:30.
    private static readonly Regex IsoTimestamp = new(
        @"^(?<date>[0-9]{4}-[0-9]{2}-[0-9]{2})[ T](?<time>[0-9]{2}:[0-9]{2}(?::[0-9]{2})?)(?<zone>Z|[+-][0-9]{2}:[0-9]{2})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] WallClockFormats = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss"];

    // Meter exports from Indian utilities write local wall-clock time, so a
    // timestamp with no offset is read as IST. One with "Z" or an explicit offset
    // is taken as written. Either way the result is returned in UTC for storage.
    // Returns null on success, otherwise the rejection reason.
    private static string? TryParseTimestamp(string value, out DateTimeOffset result)
    {
        result = default;

        var match = IsoTimestamp.Match(value.Trim());
        if (!match.Success)
        {
            return TimestampFormatReason;
        }

        // Right shape, but the values themselves may still be impossible (month 13, 25:70).
        var wallClock = $"{match.Groups["date"].Value} {match.Groups["time"].Value}";
        if (!DateTime.TryParseExact(wallClock, WallClockFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
            !TryReadOffset(match.Groups["zone"].Value, out var offset))
        {
            return InvalidTimestampReason;
        }

        try
        {
            result = new DateTimeOffset(parsed, offset).ToUniversalTime();
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Only reachable at the very ends of the DateTime range.
            return InvalidTimestampReason;
        }
    }

    private static bool TryReadOffset(string zone, out TimeSpan offset)
    {
        switch (zone)
        {
            case "":
                offset = TimeZoneConstants.IstOffset;
                return true;
            case "Z":
                offset = TimeSpan.Zero;
                return true;
        }

        // "+05:30" / "-03:00": real offsets never exceed 14 hours.
        var hours = int.Parse(zone.AsSpan(1, 2), CultureInfo.InvariantCulture);
        var minutes = int.Parse(zone.AsSpan(4, 2), CultureInfo.InvariantCulture);
        offset = new TimeSpan(hours, minutes, 0);
        if (zone[0] == '-')
        {
            offset = -offset;
        }

        return minutes < 60 && offset.Duration() <= TimeSpan.FromHours(14);
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
