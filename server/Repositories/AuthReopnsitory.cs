using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models.Entities;
namespace PortfolioHub.Server.Repositories;
public class AuthReopnsitory(AppDbContext db, UserManager<ApplicationUser> users) : IAuthReopnsitory
{
    public Task<ApplicationUser?> FindByEmail(string email) => users.FindByEmailAsync(email);
    public Task<ApplicationUser?> FindById(string userId) => users.FindByIdAsync(userId);
    public Task<bool> CheckPassword(ApplicationUser user, string password) => users.CheckPasswordAsync(user, password);
    public Task<IList<string>> GetRoles(ApplicationUser user) => users.GetRolesAsync(user);
    public Task<IdentityResult> ChangePassword(ApplicationUser user, string currentPassword, string newPassword) =>
        users.ChangePasswordAsync(user, currentPassword, newPassword);
    public Task<CreatorProfiles?> GetProfile(string userId) =>
        db.CreatorProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.IdentityUserId == userId);

    public async Task<IdentityResult> CreateAccount(ApplicationUser user, string password, CreatorProfiles profile)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) return result;
        result = await users.AddToRoleAsync(user, "Creator");
        if (!result.Succeeded) return result;
        profile.IdentityUserId = user.Id;
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return IdentityResult.Success;
    }

    public async Task<bool> UpdateProfile(string userId, string displayName, string? phone, string? avatarUrl, string bio)
    {
        // 指定更新欄位，避免和接案狀態的獨立請求互相覆寫。
        var count = await db.CreatorProfiles.Where(p => p.IdentityUserId == userId && p.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DisplayName, displayName)
                .SetProperty(p => p.ContactPhone, phone).SetProperty(p => p.AvatarUrl, avatarUrl)
                .SetProperty(p => p.Bio, bio).SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
        return count == 1;
    }
    public async Task<bool> UpdateWorkStatus(string userId, int workStatus) =>
        await db.CreatorProfiles.Where(p => p.IdentityUserId == userId && p.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.WorkStatus, workStatus)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow)) == 1;
    private const string Provider = "PortfolioHub.RevokedJwt";
    public Task<bool> IsRevoked(string userId, string tokenId) =>
        db.UserTokens.AnyAsync(t => t.UserId == userId && t.LoginProvider == Provider && t.Name == tokenId);

    public async Task Revoke(string userId, string tokenId, long expiresAt)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var previous = await db.UserTokens
            .Where(t => t.UserId == userId && t.LoginProvider == Provider).ToListAsync();
        db.UserTokens.RemoveRange(previous.Where(t => long.TryParse(t.Value, out var expiry) && expiry <= now));
        if (!previous.Any(t => t.Name == tokenId))
            db.UserTokens.Add(new IdentityUserToken<string>
            {
                UserId = userId, LoginProvider = Provider, Name = tokenId,
                Value = expiresAt.ToString(CultureInfo.InvariantCulture)
            });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // Concurrent logout requests may try to insert the same revocation.
            db.ChangeTracker.Clear();
            if (!await IsRevoked(userId, tokenId)) throw;
        }
    }
}
