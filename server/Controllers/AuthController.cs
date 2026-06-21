using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PortfolioHub.Server.Services;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace PortfolioHub.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //登入
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] RequestLoginRegisterDto request)
        {
            var result = await _authService.Login(request);
            if (result.Message == "帳號不存在")
            {
                return NotFound(result);
            }

            if (result.Message == "密碼錯誤")
            {
                return Unauthorized(result);
            }

            return Ok(result);


        }

    }
}