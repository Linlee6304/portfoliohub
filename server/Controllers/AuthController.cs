using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Services;
namespace PortfolioHub.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] RequestLoginRegisterDto request)
    {
        var result = await authService.Login(request);
        if (result.Success) return Ok(result);
        return result.Message == "帳號不存在" ? NotFound(result) : Unauthorized(result);
    }

    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RequestLoginRegisterDto request)
    {
        var result = await authService.Register(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await authService.GetCurrentUser(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize, HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] RequestUpdateProfileDto request)
    {
        var result = await authService.UpdateAccount(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize, HttpPatch("work-status")]
    public async Task<IActionResult> UpdateWorkStatus([FromBody] RequestWorkStatusDto request)
    {
        var result = await authService.UpdateWorkStatus(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] RequestChangePasswordDto request)
    {
        var result = await authService.ChangePassword(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expiration = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (userId is null || tokenId is null || !long.TryParse(expiration, out var expiresAt))
            return Unauthorized(new ResponseAuthDto { Message = "登入憑證無效，請重新登入" });
        await authService.Logout(userId, tokenId, expiresAt);
        return Ok(new ResponseAuthDto { Success = true, Message = "已登出" });
    }
}
