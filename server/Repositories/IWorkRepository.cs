using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories;

public enum WorkWriteResult { Success, NotFound, SharedWork }
public record WorkUpdateResult(Works? Work, bool InvalidMedia = false);

/// <summary>作品資料存取契約：負責 EF 查詢、持久化及交易；不處理 HTTP 或畫面訊息。</summary>
public interface IWorkRepository
{
    /// <summary>依伺服器取得的登入帳戶查詢啟用中的創作者；不存在時回傳 null，不自動建檔。</summary>
    Task<int?> FindCreatorIdAsync(string userId, CancellationToken cancellationToken);
    /// <summary>只讀取與指定創作者有關聯的作品，依更新時間及作品 ID 由新至舊排列。</summary>
    Task<List<Works>> ListAsync(int creatorId, CancellationToken cancellationToken);
    /// <summary>只讀取指定創作者的作品；不存在或不屬於該創作者均回傳 null。</summary>
    Task<Works?> FindAsync(int creatorId, int workId, CancellationToken cancellationToken);
    /// <summary>原子儲存新作品、媒體及創作者關聯；成功後回傳資料庫產生的 ID，失敗全部回滾。</summary>
    Task<Works> CreateAsync(int creatorId, Works work, CancellationToken cancellationToken);
    /// <summary>重新檢查作品與媒體歸屬，原子更新作品及媒體；不修改 WorkFeatures。
    /// replaceMedia=false 保留媒體，true 按 changes.WorkMedia 同步新增／更新／刪除及順序。
    /// 既有媒體保留 ID 與建立時間。作品不可操作回傳 Work=null；媒體 ID 非本作品回傳 InvalidMedia=true，均不寫入。</summary>
    Task<WorkUpdateResult> UpdateAsync(int creatorId, int workId, Works changes,
        bool replaceMedia, CancellationToken cancellationToken);
    /// <summary>同一交易檢查並刪除全部指定作品；任何作品不屬於本人或有其他共同創作者即全部不刪。
    /// 成功時沿用資料庫既有的媒體、功能及關聯級聯刪除規則；輸入須為非空且不重複的 ID。</summary>
    Task<WorkWriteResult> DeleteAsync(int creatorId, int[] workIds, CancellationToken cancellationToken);
}
