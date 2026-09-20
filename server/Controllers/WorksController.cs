using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Services;

namespace PortfolioHub.Server.Controllers;

[ApiController, Route("api/works"), Authorize]
public class WorksController(IWorkService service) : ControllerBase
{
    // 身分只從已驗證憑證取得，不接受 URL 或表單指定別人的創作者 ID。
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(UserId, ct));

    [HttpGet("{workId:int}")]
    public async Task<IActionResult> Get(int workId, CancellationToken ct)
    {
        var result = await service.GetAsync(UserId, workId, ct);
        return result.Code == WorkResultCode.Success ? Ok(result.Value) : Failure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveWorkDto request, CancellationToken ct)
    {
        var result = await service.CreateAsync(UserId, request, ct);
        return result.Code == WorkResultCode.Success
            ? CreatedAtAction(nameof(Get), new { workId = result.Value!.WorkId }, result.Value) : Failure(result);
    }

    [HttpPut("{workId:int}")]
    public async Task<IActionResult> Update(int workId, SaveWorkDto request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(UserId, workId, request, ct);
        return result.Code == WorkResultCode.Success ? Ok(result.Value) : Failure(result);
    }

    [HttpPost("delete-batch")]
    public async Task<IActionResult> Delete(DeleteWorksDto request, CancellationToken ct)
    {
        var result = await service.DeleteAsync(UserId, request, ct);
        return result.Code == WorkResultCode.Success ? NoContent() : Failure(result);
    }

    private ObjectResult Failure<T>(WorkResult<T> result) => StatusCode(result.Code switch
    {
        WorkResultCode.NotFound => StatusCodes.Status404NotFound,
        WorkResultCode.MissingProfile or WorkResultCode.SharedWork => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    }, new { message = result.Message });
}
