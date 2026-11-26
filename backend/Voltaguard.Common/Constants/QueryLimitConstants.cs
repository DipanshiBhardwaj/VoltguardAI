namespace Voltaguard.Common.Constants;

/// <summary>
/// Row limits and windows for read queries, so one request can never pull an
/// unbounded number of readings and the forecast always uses the same window.
/// </summary>
public static class QueryLimitConstants
{
    // "Latest readings" list without a date range.
    public const int DefaultRecentReadings = 100;
    public const int MaxRecentReadings = 500;

    // Readings for a date range (consumption charts). A full 31-day month of
    // 15-minute readings is 2,976 rows, so one month always fits.
    public const int MaxRangeReadings = 3000;

    // The bill projection averages this many most recent days of readings.
    public const int ForecastWindowDays = 30;
}
