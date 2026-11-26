using Voltaguard.Common.Constants;
using Voltaguard.Common.Dtos;
using Voltaguard.Common.Enums;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class IngestionService : IIngestionService
{
    public const string DuplicateReadingReason = "Duplicate reading for this time interval.";
    public const string UnsupportedFileTypeReason = "Only .csv or .txt ledger files can be uploaded.";
    public const string BinaryFileReason = "The file is not plain text. Export the ledger as CSV and try again.";

    private readonly IMeterRepository _meterRepository;
    private readonly ITelemetryRepository _telemetryRepository;

    public IngestionService(IMeterRepository meterRepository, ITelemetryRepository telemetryRepository)
    {
        _meterRepository = meterRepository;
        _telemetryRepository = telemetryRepository;
    }

    public async Task<IngestionResultDto> IngestAsync(
        int meterAssetId,
        int ownerUserId,
        string fileName,
        Stream fileStream,
        long fileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(fileStream);

        // Boundary guard: reject an oversized, wrongly typed or binary file before
        // reading a single line.
        if (fileSizeBytes > ValidationLimitConstants.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The uploaded file is larger than the allowed limit.");
        }

        if (!ValidationLimitConstants.AllowedFileExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(UnsupportedFileTypeReason);
        }

        fileStream = await EnsureSeekableAsync(fileStream, cancellationToken);
        if (await StartsWithBinaryContentAsync(fileStream, cancellationToken))
        {
            throw new InvalidOperationException(BinaryFileReason);
        }

        // Ownership guard: the meter must belong to the caller.
        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("Meter not found for the current user.");
        }

        // Rows that pass the per-line checks, kept with their line number so the
        // duplicate check below can still report where each one came from.
        var parsed = new List<(int LineNumber, TelemetryLedger Reading)>();
        var errors = new List<RowErrorDto>();

        using var reader = new StreamReader(fileStream);
        var lineNumber = 0;
        var isFirstLine = true;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            // Skip a header row if the file has one, and ignore blank lines.
            if (isFirstLine && LooksLikeHeader(line))
            {
                isFirstLine = false;
                continue;
            }
            isFirstLine = false;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var outcome = TelemetryRowParser.Parse(line, meterAssetId);
            if (outcome.IsAccepted)
            {
                parsed.Add((lineNumber, outcome.Reading!));
            }
            else
            {
                errors.Add(new RowErrorDto { LineNumber = lineNumber, Reason = outcome.Reason! });
            }
        }

        var accepted = await RemoveDuplicatesAsync(meterAssetId, parsed, errors, cancellationToken);
        errors.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));

        // Classify the rows being saved. A current mismatch is the tamper signal,
        // so it is also flagged on the stored reading.
        int sags = 0, swells = 0, mismatches = 0;
        foreach (var reading in accepted)
        {
            foreach (var anomaly in ReadingAnomalyDetector.Detect(reading))
            {
                switch (anomaly)
                {
                    case AnomalyType.VoltageSag: sags++; break;
                    case AnomalyType.VoltageSwell: swells++; break;
                    case AnomalyType.CurrentMismatch:
                        mismatches++;
                        reading.TamperFlag = true;
                        break;
                }
            }
        }

        if (accepted.Count > 0)
        {
            await _telemetryRepository.AddRangeAsync(accepted, cancellationToken);
            await _telemetryRepository.SaveChangesAsync(cancellationToken);
        }

        return new IngestionResultDto
        {
            AcceptedRows = accepted.Count,
            RejectedRows = errors.Count,
            Errors = errors,
            AnomaliesDetected = sags + swells + mismatches,
            VoltageSagCount = sags,
            VoltageSwellCount = swells,
            CurrentMismatchCount = mismatches
        };
    }

    // The upload stream may be forward-only. Files are at most 10 MB, so copying
    // one into memory to allow a second pass is cheap.
    private static async Task<Stream> EnsureSeekableAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    // Text never contains a NUL byte; spreadsheets, images and other binaries do.
    private static async Task<bool> StartsWithBinaryContentAsync(Stream stream, CancellationToken cancellationToken)
    {
        var start = stream.Position;
        var head = new byte[ValidationLimitConstants.BinaryCheckBytes];
        var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);
        stream.Position = start;

        return Array.IndexOf(head, (byte)0, 0, read) >= 0;
    }

    // Rejects a reading whose interval is already stored for this meter, or that
    // appeared earlier in the same file. Existing intervals are fetched with one
    // query covering the file's time range rather than one query per row.
    private async Task<List<TelemetryLedger>> RemoveDuplicatesAsync(
        int meterAssetId,
        List<(int LineNumber, TelemetryLedger Reading)> parsed,
        List<RowErrorDto> errors,
        CancellationToken cancellationToken)
    {
        var accepted = new List<TelemetryLedger>();
        if (parsed.Count == 0)
        {
            return accepted;
        }

        var earliest = parsed.Min(p => p.Reading.IntervalEnding);
        var latest = parsed.Max(p => p.Reading.IntervalEnding);
        var stored = await _telemetryRepository.GetExistingIntervalsAsync(meterAssetId, earliest, latest, cancellationToken);

        var seen = new HashSet<DateTimeOffset>(stored);
        foreach (var (lineNumber, reading) in parsed)
        {
            if (seen.Add(reading.IntervalEnding))
            {
                accepted.Add(reading);
            }
            else
            {
                errors.Add(new RowErrorDto { LineNumber = lineNumber, Reason = DuplicateReadingReason });
            }
        }

        return accepted;
    }

    // A header row has a non-numeric second column where a voltage reading would be.
    private static bool LooksLikeHeader(string line)
    {
        var cells = line.Split(',');
        if (cells.Length < 2)
        {
            return false;
        }

        return !decimal.TryParse(cells[1].Trim(), out _);
    }
}
