using PortfolioHub.Server.DTOs;
namespace PortfolioHub.Server.Services;
public interface IAuthService
{
    /// <summary>驗證信箱及密碼，回傳帳戶資料與登入憑證。</summary>
    Task<ResponseAuthInfoDto> Login(RequestLoginRegisterDto request);
    /// <summary>註冊一般使用者，角色固定為 Creator，無法指定管理員。</summary>
    Task<ResponseAuthDto> Register(RequestLoginRegisterDto request);
    /// <summary>取得目前已登入使用者的帳戶與基本資料。</summary>
    Task<ResponseGetAccountDto> GetCurrentUser(string userId);
    /// <summary>只更新自己的暱稱、電話、頭像與簡介。</summary>
    Task<ResponseGetAccountDto> UpdateAccount(string userId, RequestUpdateProfileDto request);
    /// <summary>立即儲存自己的接案狀態：0 不接案、1 接案中、2 可接案。</summary>
    Task<ResponseWorkStatusDto> UpdateWorkStatus(string userId, RequestWorkStatusDto request);
    /// <summary>單獨修改密碼，成功後使該帳戶所有既有登入失效。</summary>
    Task<ResponseChangePasswordDto> ChangePassword(string userId, RequestChangePasswordDto request);
    /// <summary>登出目前憑證，保留其他裝置的登入。</summary>
    Task Logout(string userId, string tokenId, long expiresAt);
    /// <summary>檢查帳戶、安全戳記及撤銷紀錄，確認登入仍有效。</summary>
    Task<bool> IsSessionValid(string? userId, string? tokenId, string? stamp);
}
