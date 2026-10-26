using Voltaguard.Common.Dtos;

namespace Voltaguard.BLL.Services;

public interface IIngestionService
{
    /// <summary>
    /// Parses an uploaded meter ledger line-by-line, saving valid rows against the
    /// meter and returning a per-line report of what was accepted and rejected.
    /// </summary>
    Task<IngestionResultDto> IngestAsync(
        int meterAssetId,
        int ownerUserId,
        Stream fileStream,
        long fileSizeBytes,
        CancellationToken cancellationToken = default);
}
