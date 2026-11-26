namespace Voltaguard.DAL.Configurations;

/// <summary>Builds the quoted value list for an SQL "IN (...)" check constraint.</summary>
internal static class SqlList
{
    // Values come from enum names and constants, never user input; single quotes
    // are still doubled so the SQL is always well-formed.
    public static string Of(IEnumerable<string> values) =>
        string.Join(", ", values.Select(v => $"'{v.Replace("'", "''")}'"));
}
