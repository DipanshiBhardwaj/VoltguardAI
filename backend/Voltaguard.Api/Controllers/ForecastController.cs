using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Dtos;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meters/{meterAssetId:int}/forecast")]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _forecastService;

    public ForecastController(IForecastService forecastService)
    {
        _forecastService = forecastService;
    }

    /// <summary>
    /// Projects next month's bill from the average daily use of the meter's last
    /// 30 days of readings, priced against its tariff slabs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BillForecastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BillForecastDto>> Get(int meterAssetId, CancellationToken cancellationToken)
    {
        var forecast = await _forecastService.GetForecastAsync(meterAssetId, User.GetUserId(), cancellationToken);
        return Ok(forecast);
    }
}
