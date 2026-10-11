using PortfolioHub.Server.DTOs;

namespace PortfolioHub.Server.Services;

/// <summary>作品業務契約：將登入帳戶對應創作者，驗證資料並呼叫 Repository；不直接操作資料庫。</summary>
public interface IWorkService
{
    /// <summary>userId 必須來自已驗證的登入憑證；無創作者時回傳 HasCreatorProfile=false 與空列表。</summary>
    Task<WorkListDto> ListAsync(string userId, CancellationToken cancellationToken);
    /// <summary>查詢本人有關聯的作品；不存在及他人作品都回傳 NotFound，不洩漏他人資料。</summary>
    Task<WorkResult<WorkDto>> GetAsync(string userId, int workId, CancellationToken cancellationToken);
    /// <summary>驗證名稱、日期、作品類型 1–3、狀態 0/1，以及最多 50 筆 HTTP(S) 媒體連結後原子建立。
    /// 媒體類型 1=YOUTUBE、2=GIT、3=文件；功能最多 50 筆、名稱 1–200 字、介紹最多 5000 字；
    /// 日期可空，兩者有值時結束不可早於開始。功能狀態 0–4、整數進度 0–100 獨立編輯，不自動連動。
    /// 新增功能與媒體 ID 必須為 0；缺少創作者回傳 MissingProfile。</summary>
    Task<WorkResult<WorkDto>> CreateAsync(string userId, SaveWorkDto request, CancellationToken cancellationToken);
    /// <summary>後台編輯契約：驗證同建立，只更新本人有關聯作品與所屬子項目。
    /// features/media 分別省略或 null 即保留，空陣列則清空對應集合，互不影響。
    /// 指定子項目 ID 必須為本作品既有資料；媒體次序決定 SortOrder，功能依 FeatureId 排序。
    /// 功能 Introduction 對應實體 Remarks／資料庫 Remark。任何錯誤不部分儲存。</summary>
    Task<WorkResult<WorkDto>> UpdateAsync(string userId, int workId, SaveWorkDto request, CancellationToken cancellationToken);
    /// <summary>驗證 1–100 個正整數 ID 後原子刪除；共同作品不刪除，任一項失敗整批保留。</summary>
    Task<WorkResult<bool>> DeleteAsync(string userId, DeleteWorksDto request, CancellationToken cancellationToken);
}
