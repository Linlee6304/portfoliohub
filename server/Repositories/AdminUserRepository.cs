using System.Data;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models.Entities;
namespace PortfolioHub.Server.Repositories;

public class AdminUserRepository(AppDbContext db) : IAdminUserRepository
{
    public Task<bool> IsAdmin(string userId) => db.UserRoles.AnyAsync(ur => ur.UserId == userId &&
        db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));

    private IQueryable<CreatorProfiles> OrdinaryUsers() => db.CreatorProfiles.Where(p =>
        db.UserRoles.Any(ur => ur.UserId == p.IdentityUserId && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Creator")) &&
        !db.UserRoles.Any(ur => ur.UserId == p.IdentityUserId && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")));

    public Task<List<CreatorProfiles>> ListUsers() => OrdinaryUsers().AsNoTracking()
        .Include(p => p.IdentityUser).OrderBy(p => p.DisplayName).ThenBy(p => p.CreatorId).ToListAsync();

    public async Task<bool> SetActive(string userId, bool isActive)
    {
        // 在同一交易內確認目標角色、更新狀態與安全戳記，避免只完成其中一步。
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var profile = await OrdinaryUsers().SingleOrDefaultAsync(p => p.IdentityUserId == userId);
        if (profile is null) return false;
        if (profile.IsActive != isActive)
        {
            profile.IsActive = isActive;
            profile.UpdatedAt = DateTime.UtcNow;
            if (!isActive)
            {
                var stamp = Guid.NewGuid().ToString();
                var concurrency = Guid.NewGuid().ToString();
                await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s =>
                    s.SetProperty(u => u.SecurityStamp, stamp).SetProperty(u => u.ConcurrencyStamp, concurrency));
            }
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return true;
    }
}
