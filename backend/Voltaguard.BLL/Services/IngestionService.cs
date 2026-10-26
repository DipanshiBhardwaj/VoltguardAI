using Voltaguard.Common.Constants;
using Voltaguard.Common.Dtos;
using Voltaguard.DAL.Entities;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL.Services;

public class IngestionService : IIngestionService
{
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
        Stream fileStream,
        long fileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        // Boundary guard: reject an oversized file before reading a single line.
        if (fileSizeBytes > ValidationLimitConstants.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The uploaded file is larger than the allowed limit.");
        }

        // Ownership guard: the meter must belong to the caller.
        var meter = await _meterRepository.GetOwnedByIdAsync(meterAssetId, ownerUserId, cancellationToken);
        if (meter is null)
        {
            throw new InvalidOperationException("Meter not found for the current user.");
        }

        var accepted = new List<TelemetryLedger>();
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
                accepted.Add(outcome.Reading!);
            }
            else
            {
                errors.Add(new RowErrorDto { LineNumber = lineNumber, Reason = outcome.Reason! });
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
            Errors = errors
        };
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
