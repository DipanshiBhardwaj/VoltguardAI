using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
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

    /// <summary>Returns the most recent readings for a meter, newest first.</summary>
    [HttpGet("{meterAssetId:int}/readings")]
    [ProducesResponseType(typeof(List<TelemetryRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TelemetryRowDto>>> GetReadings(
        int meterAssetId,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var limit = take is > 0 and <= 500 ? take : 100;
        var rows = await _meterService.GetRecentReadingsAsync(meterAssetId, User.GetUserId(), limit, cancellationToken);
        return Ok(rows);
    }
}
