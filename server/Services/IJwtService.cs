using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Services
{
    public interface IJwtService
    {
        /// <summary>依帳戶及資料庫角色簽發含安全戳記、識別碼與期限的 JWT。</summary>
        string GenerateToken(
            ApplicationUser user,
            IList<string> roles);
    }
}