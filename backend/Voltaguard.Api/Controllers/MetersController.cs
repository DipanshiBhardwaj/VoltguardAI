using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Constants;
using Voltaguard.Common.Dtos;
using Voltaguard.Common.Requests;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meters")]
public class MetersController : ControllerBase
{
    private readonly IMeterService _meterService;

    public MetersController(IMeterService meterService)
    {
        _meterService = meterService;
    }

    /// <summary>Lists the signed-in user's meters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MeterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MeterDto>>> GetMine(CancellationToken cancellationToken)
    {
        var meters = await _meterService.GetMyMetersAsync(User.GetUserId(), cancellationToken);
        return Ok(meters);
    }

    /// <summary>Registers a new meter under the signed-in user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MeterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeterDto>> Create([FromBody] CreateMeterRequest request, CancellationToken cancellationToken)
    {
        var meter = await _meterService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetMine), new { }, meter);
    }

    /// <summary>Changes a meter's serial number and tariff category.</summary>
    [HttpPut("{meterAssetId:int}")]
    [ProducesResponseType(typeof(MeterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeterDto>> Update(
        int meterAssetId,
        [FromBody] UpdateMeterRequest request,
        CancellationToken cancellationToken)
    {
        var meter = await _meterService.UpdateAsync(meterAssetId, User.GetUserId(), request, cancellationToken);
        return Ok(meter);
    }

    /// <summary>Removes a meter (soft delete; its readings and reports are kept).</summary>
    [HttpDelete("{meterAssetId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int meterAssetId, CancellationToken cancellationToken)
    {
        await _meterService.DeleteAsync(meterAssetId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Returns a meter's readings, newest first. Without from/to, the most recent
    /// readings (up to 500). With a from and/or to date, the readings in that range
    /// (up to 3,000, enough for a full month of 15-minute readings).
    /// </summary>
    [HttpGet("{meterAssetId:int}/readings")]
    [ProducesResponseType(typeof(List<TelemetryRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TelemetryRowDto>>> GetReadings(
        int meterAssetId,
        [FromQuery] int take,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (from is null && to is null)
        {
            var limit = take is > 0 and <= QueryLimitConstants.MaxRecentReadings ? take : QueryLimitConstants.DefaultRecentReadings;
            var recent = await _meterService.GetRecentReadingsAsync(meterAssetId, User.GetUserId(), limit, cancellationToken);
            return Ok(recent);
        }

        var rangeLimit = take is > 0 and <= QueryLimitConstants.MaxRangeReadings ? take : QueryLimitConstants.MaxRangeReadings;
        var rows = await _meterService.GetReadingsInRangeAsync(meterAssetId, User.GetUserId(), from, to, rangeLimit, cancellationToken);
        return Ok(rows);
    }

    /// <summary>
    /// Abnormal readings for a meter (voltage sag/swell, phase/neutral current
    /// mismatch) in an optional date range, newest first.
    /// </summary>
    [HttpGet("{meterAssetId:int}/anomalies")]
    [ProducesResponseType(typeof(List<AnomalyReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AnomalyReadingDto>>> GetAnomalies(
        int meterAssetId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var rows = await _meterService.GetAnomaliesAsync(meterAssetId, User.GetUserId(), from, to, cancellationToken);
        return Ok(rows);
    }
}
