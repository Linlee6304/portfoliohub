using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Services;
namespace PortfolioHub.Server.Controllers;

[ApiController, Route("api/admin/users"), Authorize(Roles = "Admin")]
public class AdminUsersController(IAdminUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!await service.CanManage(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return Forbid();
        return Ok(await service.ListUsers());
    }

    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> SetStatus(string userId, RequestAccountStatusDto request)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await service.CanManage(actor)) return Forbid();
        if (!await service.SetActive(actor, userId, request.IsActive!.Value))
            return NotFound(new ResponseAuthDto { Message = "找不到可管理的一般使用者" });
        return Ok(new { success = true, message = "帳戶狀態已儲存", identityUserId = userId, isActive = request.IsActive.Value });
    }
}
