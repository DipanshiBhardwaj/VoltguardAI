using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;

namespace Voltaguard.BLL.Services;

public interface IMeterService
{
    Task<List<MeterDto>> GetMyMetersAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<MeterDto> CreateAsync(int ownerUserId, CreateMeterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the serial number and tariff category of one of the caller's meters.</summary>
    Task<MeterDto> UpdateAsync(
        int meterAssetId,
        int ownerUserId,
        UpdateMeterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes one of the caller's meters.</summary>
    Task DeleteAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<List<TelemetryRowDto>> GetRecentReadingsAsync(
        int meterAssetId,
        int ownerUserId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Readings in an optional date range (either end may be open), newest first.</summary>
    Task<List<TelemetryRowDto>> GetReadingsInRangeAsync(
        int meterAssetId,
        int ownerUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Abnormal readings in an optional date range, newest first, one entry per anomaly type.</summary>
    Task<List<AnomalyReadingDto>> GetAnomaliesAsync(
        int meterAssetId,
        int ownerUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);
}
