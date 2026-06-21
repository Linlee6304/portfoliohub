using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PortfolioHub.Server.Repositories
{
    public class AuthReopnsitory : IAuthReopnsitory
    {
        private readonly AppDbContext _context;
        public AuthReopnsitory(AppDbContext context)
        {
            _context = context;
        }

        public Task CreateCreatorProfile(CreatorProfiles profile)//創建創作者資料
        {
            _context.CreatorProfiles.Add(profile);
            return _context.SaveChangesAsync();
        }
        public async Task<ResponseGetAccountDto> GetAccountByEmail(string email)
        {
            //邏輯整理:根據email查詢用戶IdentityUserId，然後根據IdentityUserId jion查詢CreatorProfiles，最後組裝ResponseGetAccountDto回傳
            var result = await _context.CreatorProfiles
                .Where(x => x.IdentityUser.Email == email)
                .Select(x => new ResponseGetAccountDto
                {
                    DisplayName = x.DisplayName,
                    ContactEmail = x.ContactEmail,
                    ContactPhone = x.ContactPhone,
                    AvatarUrl = x.AvatarUrl,
                    Bio = x.Bio
                })
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<bool> IsEmailExists(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}