using Voltaguard.Common.Dtos;

namespace Voltaguard.BLL.Services;

public interface IForecastService
{
    /// <summary>
    /// Projects the bill for the calendar month after the meter's latest reading
    /// from the average daily use of its most recent readings. Deterministic, no AI.
    /// </summary>
    Task<BillForecastDto> GetForecastAsync(int meterAssetId, int ownerUserId, CancellationToken cancellationToken = default);
}
