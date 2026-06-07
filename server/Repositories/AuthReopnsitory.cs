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

        public Task CreateCreatorProfile(CreatorProfiles profile)
        {
            _context.CreatorProfiles.Add(profile);
            return _context.SaveChangesAsync();
        }

        public async Task<bool> IsEmailExists(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}