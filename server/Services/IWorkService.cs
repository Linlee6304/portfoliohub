using PortfolioHub.Server.DTOs;

namespace PortfolioHub.Server.Services;

/// <summary>作品業務契約：將登入帳戶對應創作者，驗證資料並呼叫 Repository；不直接操作資料庫。</summary>
public interface IWorkService
{
    /// <summary>userId 必須來自已驗證的登入憑證；無創作者時回傳 HasCreatorProfile=false 與空列表。</summary>
    Task<WorkListDto> ListAsync(string userId, CancellationToken cancellationToken);
    /// <summary>查詢本人有關聯的作品；不存在及他人作品都回傳 NotFound，不洩漏他人資料。</summary>
    Task<WorkResult<WorkDto>> GetAsync(string userId, int workId, CancellationToken cancellationToken);
    /// <summary>驗證必填名稱、長度及日期順序後建立作品與本人關聯；缺少創作者回傳 MissingProfile，不自動建立。</summary>
    Task<WorkResult<WorkDto>> CreateAsync(string userId, SaveWorkDto request, CancellationToken cancellationToken);
    /// <summary>只更新本人有關聯作品的基本四欄；失敗不應回傳成功，未開放欄位保留既有值。</summary>
    Task<WorkResult<WorkDto>> UpdateAsync(string userId, int workId, SaveWorkDto request, CancellationToken cancellationToken);
    /// <summary>驗證 1–100 個正整數 ID 後原子刪除；共同作品不刪除，任一項失敗整批保留。</summary>
    Task<WorkResult<bool>> DeleteAsync(string userId, DeleteWorksDto request, CancellationToken cancellationToken);
}
