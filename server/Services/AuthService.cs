using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace PortfolioHub.Server.Services
{
    public class AuthService : IAuthService//帳號相關邏輯
    {
        private readonly IAuthReopnsitory _authRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        public AuthService(IAuthReopnsitory authRepository, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _authRepository = authRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }
        public async Task<ResponseAuthInfoDto> Login(RequestLoginRegisterDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ResponseAuthInfoDto
                {
                    Message = "帳號不存在"
                };
            }
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                return new ResponseAuthInfoDto
                {
                    Message = "密碼錯誤"
                };
            }
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return new ResponseAuthInfoDto
                {
                    Message = "管理員登入成功",
                    Role = "Admin",
                    DisplayName = user.UserName,
                    AvatarUrl = null//管理員沒有頭像，未來可以考慮增加管理員頭像，目前我就直接給null
                };
            }
            if (roles.Contains("Creator"))
            {
                var profile = await _context.CreatorProfiles
                    .FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);

                if (profile == null)
                {
                    return new ResponseAuthInfoDto
                    {
                        Message = "創作者資料不存在，請聯繫管理員",
                        Role = "Creator"
                    };
                }

                return new ResponseAuthInfoDto
                {
                    Message = "創作者登入成功",
                    Role = "Creator",
                    DisplayName = profile.DisplayName,
                    AvatarUrl = string.IsNullOrWhiteSpace(profile.AvatarUrl)
                        ? "xxx" // 預設頭像
                        : profile.AvatarUrl
                };
            }
            if (roles.Count == 0)
            {
                return new ResponseAuthInfoDto
                {
                    Message = "此帳號未分配角色，請聯繫管理員"
                };
            }
            return new ResponseAuthInfoDto
            {
                Message = "創作者登入成功",
                Role = "Creator"
            };
        }

        public async Task<ResponseAuthDto> Register(RequestLoginRegisterDto request)
        {
            #region 驗證註冊資料
            var creator = await _authRepository.IsEmailExists(request.Email);

            if (creator)
            {
                return new ResponseAuthDto
                {
                    Message = "此信箱已存在於會員資料中"
                };
            }
            var identityuser = await _userManager.FindByEmailAsync(request.Email);
            if (identityuser != null)
            {
                return new ResponseAuthDto
                {
                    Message = "此信箱已註冊登入帳號"
                };
            }


            if (request.Password != request.ConfirmPassword)
            {
                return new ResponseAuthDto
                {
                    Message = "帳號密碼請輸入一致"
                };
            }

            if (request.Password.Length < 6)
            {
                return new ResponseAuthDto
                {
                    Message = "密碼長度至少6個字元"//我不做出過多密碼限制
                };
            }
            #endregion
            #region 開始註冊
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email
                };

                var result = await _userManager.CreateAsync(
                    user,
                    request.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(error.Code);
                        Console.WriteLine(error.Description);
                    }
                    return new ResponseAuthDto
                    {
                        Message = "註冊失敗，請稍後再試"
                    };
                }
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Creator");//預設註冊的帳號都是創作者，未來可以考慮增加管理員帳號註冊，目前我就直接對資料庫創建管理者帳號

                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    return new ResponseAuthDto
                    {
                        Message = "角色指派失敗"
                    };
                }
                var profile = new CreatorProfiles
                {
                    IdentityUserId = user.Id,
                    DisplayName = request.DisplayName ?? string.Empty,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Bio = string.Empty,
                    AvatarUrl = null,
                    IsActive = true,
                    WorkStatus = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _authRepository.CreateCreatorProfile(profile);
                await transaction.CommitAsync();


                return new ResponseAuthDto
                {
                    Message = "註冊成功"
                };
            }
            catch
            {
                await transaction.RollbackAsync();

                return new ResponseAuthDto
                {
                    Message = "註冊失敗"
                };
            }
            #endregion
        }
        public async Task<ResponseAuthDto> GetAccountByEmail(string email)//用註冊信箱查詢帳號
        {
            throw new NotImplementedException();
        }

        public async Task<ResponseAuthDto> UpdateAccount(RequestAuthDto request)//更新帳號資料
        {
            //邏輯梳理:先查詢
            throw new NotImplementedException();
        }
    }
}