using PortfolioHub.Server.Models.Entities;
namespace PortfolioHub.Server.Repositories;
public interface IAdminUserRepository
{
    /// <summary>查詢資料庫目前是否仍授予操作者 Admin 角色。</summary>
    Task<bool> IsAdmin(string userId);
    /// <summary>讀取具 Creator 角色且不具 Admin 角色的一般使用者及其帳戶資料。</summary>
    Task<List<CreatorProfiles>> ListUsers();
    /// <summary>以交易更新一般使用者啟用狀態；關閉時撤銷所有舊登入，不更新個資或角色。</summary>
    Task<bool> SetActive(string userId, bool isActive);
}
