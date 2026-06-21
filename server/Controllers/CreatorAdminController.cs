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
    public class CreatorAdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public CreatorAdminController(IAuthService authService)
        {
            _authService = authService;
        }


    }
}