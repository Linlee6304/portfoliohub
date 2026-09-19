using PortfolioHub.Server.DTOs;
namespace PortfolioHub.Server.Services;
public interface IAdminUserService
{
    /// <summary>以資料庫角色確認目前操作者仍有管理者權限，避免沿用已撤除的舊 JWT 角色。</summary>
    Task<bool> CanManage(string userId);
    /// <summary>取得一般使用者列表，只提供名稱、信箱、電話與啟用狀態。</summary>
    Task<List<AdminUserDto>> ListUsers();
    /// <summary>僅切換一般使用者啟用狀態，不允許操作自己或管理者帳戶。</summary>
    Task<bool> SetActive(string actorId, string targetId, bool isActive);
}
