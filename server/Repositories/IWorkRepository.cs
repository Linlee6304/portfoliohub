using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories;

public enum WorkWriteResult { Success, NotFound, SharedWork }

/// <summary>作品資料存取契約：負責 EF 查詢、持久化及交易；不處理 HTTP 或畫面訊息。</summary>
public interface IWorkRepository
{
    /// <summary>依伺服器取得的登入帳戶查詢啟用中的創作者；不存在時回傳 null，不自動建檔。</summary>
    Task<int?> FindCreatorIdAsync(string userId, CancellationToken cancellationToken);
    /// <summary>只讀取與指定創作者有關聯的作品，依更新時間及作品 ID 由新至舊排列。</summary>
    Task<List<Works>> ListAsync(int creatorId, CancellationToken cancellationToken);
    /// <summary>只讀取指定創作者的作品；不存在或不屬於該創作者均回傳 null。</summary>
    Task<Works?> FindAsync(int creatorId, int workId, CancellationToken cancellationToken);
    /// <summary>一次儲存新作品及創作者關聯；成功後回傳的實體含資料庫產生的作品 ID。</summary>
    Task<Works> CreateAsync(int creatorId, Works work, CancellationToken cancellationToken);
    /// <summary>重新以創作者關聯限制更新範圍；只更新基本四欄與更新時間，保留其他作品資料。</summary>
    Task<Works?> UpdateAsync(int creatorId, int workId, string title, string description,
        DateTime? startDate, DateTime? endDate, DateTime updatedAt, CancellationToken cancellationToken);
    /// <summary>同一交易檢查並刪除全部指定作品；任何作品不屬於本人或有其他共同創作者即全部不刪。
    /// 成功時沿用資料庫既有的媒體、功能及關聯級聯刪除規則；輸入須為非空且不重複的 ID。</summary>
    Task<WorkWriteResult> DeleteAsync(int creatorId, int[] workIds, CancellationToken cancellationToken);
}
