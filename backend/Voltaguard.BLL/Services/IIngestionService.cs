using Voltaguard.Common.Dtos;

namespace Voltaguard.BLL.Services;

public interface IIngestionService
{
    /// <summary>
    /// Checks the file (size, .csv/.txt extension, no binary content), then parses
    /// it line-by-line, saving valid rows against the meter and returning a
    /// per-line report of what was accepted and rejected plus any anomalies found.
    /// </summary>
    Task<IngestionResultDto> IngestAsync(
        int meterAssetId,
        int ownerUserId,
        string fileName,
        Stream fileStream,
        long fileSizeBytes,
        CancellationToken cancellationToken = default);
}
