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
        private readonly AppDbContext _context;
        public AuthService(IAuthReopnsitory authRepository, UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _authRepository = authRepository;
            _userManager = userManager;
            _context = context;
        }
        public async Task<ResponseAuthDto> Login(RequestLoginRegisterDto request)
        {
            throw new NotImplementedException();
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

        public async Task<ResponseAuthDto> UpdateAccount(RequestAuthDto request)
        {
            throw new NotImplementedException();
        }
    }
}