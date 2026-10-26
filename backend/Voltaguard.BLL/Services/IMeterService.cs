using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;

namespace Voltaguard.BLL.Services;

public interface IMeterService
{
    Task<List<MeterDto>> GetMyMetersAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<MeterDto> CreateAsync(int ownerUserId, CreateMeterRequest request, CancellationToken cancellationToken = default);

    Task<List<TelemetryRowDto>> GetRecentReadingsAsync(
        int meterAssetId,
        int ownerUserId,
        int take,
        CancellationToken cancellationToken = default);
}
