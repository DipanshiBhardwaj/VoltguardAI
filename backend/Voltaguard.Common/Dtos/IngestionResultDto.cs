namespace Voltaguard.Common.Dtos;

/// <summary>
/// Outcome of parsing an uploaded meter file: how many rows were saved, how many
/// were rejected, and the reason for each rejected line so the user can fix the file.
/// </summary>
public sealed class IngestionResultDto
{
    public int AcceptedRows { get; init; }
    public int RejectedRows { get; init; }
    public IReadOnlyList<RowErrorDto> Errors { get; init; } = new List<RowErrorDto>();
}

/// <summary>A single rejected line, identified by its position in the file.</summary>
public sealed class RowErrorDto
{
    public int LineNumber { get; init; }
    public string Reason { get; init; } = string.Empty;
}
