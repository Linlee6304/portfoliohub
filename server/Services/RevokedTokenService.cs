using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
namespace PortfolioHub.Server.Services;

// The existing Identity table persists revocations across server restarts.
// Logout revokes only the supplied JWT, leaving other sessions active.
public class RevokedTokenService(AppDbContext db)
{
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
