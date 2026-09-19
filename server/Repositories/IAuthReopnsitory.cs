using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.Models.Entities;
namespace PortfolioHub.Server.Repositories;
public interface IAuthReopnsitory
{
    /// <summary>依登入信箱查詢 Identity 帳戶。</summary>
    Task<ApplicationUser?> FindByEmail(string email);
    /// <summary>依已驗證的帳戶識別碼查詢 Identity 帳戶。</summary>
    Task<ApplicationUser?> FindById(string userId);
    /// <summary>驗證帳戶密碼，不儲存明文密碼。</summary>
    Task<bool> CheckPassword(ApplicationUser user, string password);
    /// <summary>讀取資料庫分配的角色，不提供修改角色的功能。</summary>
    Task<IList<string>> GetRoles(ApplicationUser user);
    /// <summary>以同一交易建立帳戶、固定 Creator 角色與基本資料。</summary>
    Task<IdentityResult> CreateAccount(ApplicationUser user, string password, CreatorProfiles profile);
    /// <summary>讀取基本資料，不自動新增不存在的資料。</summary>
    Task<CreatorProfiles?> GetProfile(string userId);
    /// <summary>只更新暱稱、電話、頭像、簡介，保留信箱與接案狀態。</summary>
    Task<bool> UpdateProfile(string userId, string displayName, string? phone, string? avatarUrl, string bio);
    /// <summary>只更新接案狀態及更新時間，不覆寫基本資料。</summary>
    Task<bool> UpdateWorkStatus(string userId, int workStatus);
    /// <summary>修改密碼並更新安全戳記，使舊登入憑證失效。</summary>
    Task<IdentityResult> ChangePassword(ApplicationUser user, string currentPassword, string newPassword);
    /// <summary>查詢目前登入憑證是否已登出撤銷。</summary>
    Task<bool> IsRevoked(string userId, string tokenId);
    /// <summary>持久保存目前憑證的撤銷紀錄，清理該帳戶過期紀錄。</summary>
    Task Revoke(string userId, string tokenId, long expiresAt);
}
