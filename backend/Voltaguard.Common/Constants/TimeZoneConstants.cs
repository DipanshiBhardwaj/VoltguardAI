namespace Voltaguard.Common.Constants;

/// <summary>
/// Indian Standard Time. India has no daylight saving, so one fixed offset is
/// exact all year. Timestamps are stored in UTC and converted to this offset
/// whenever an hour of day or calendar day matters (analysis and display).
/// </summary>
public static class TimeZoneConstants
{
    public static readonly TimeSpan IstOffset = new(5, 30, 0);

    // IANA name, for clients (browser, Python) that format times themselves.
    public const string IstTimeZoneId = "Asia/Kolkata";
}
