using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltaguard.Api.Extensions;
using Voltaguard.BLL.Services;
using Voltaguard.Common.Dtos;

namespace Voltaguard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meters/{meterAssetId:int}/telemetry")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionService _ingestionService;

    public IngestionController(IIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Uploads an exported meter ledger (CSV/text). The file is validated line-by-line;
    /// valid rows are saved and the response reports what was accepted and rejected.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(IngestionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngestionResultDto>> Upload(
        int meterAssetId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _ingestionService.IngestAsync(
            meterAssetId, User.GetUserId(), stream, file.Length, cancellationToken);

        return Ok(result);
    }
}
