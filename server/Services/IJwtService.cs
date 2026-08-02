using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Services
{
    public interface IJwtService
    {
        string GenerateToken(
            ApplicationUser user,
            IList<string> roles);
    }
}