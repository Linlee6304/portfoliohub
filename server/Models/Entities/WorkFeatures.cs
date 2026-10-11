using System.ComponentModel.DataAnnotations.Schema;

namespace PortfolioHub.Server.Models.Entities;

public class WorkFeatures//作品功能表
{
    public int FeatureId { get; set; }//PK,int NotNull流水號
    public int WorkId { get; set; }//FK → Works:WorkId
    public string FeatureName { get; set; } = string.Empty;//功能名稱
    public DateTime? StartDate { get; set; } //開始日期
    public DateTime? EndDate { get; set; } //結束日期
    public int Status { get; set; } //0 未開始、1 完成、2 製作中、3 暫停、4 放棄
    public byte Progress { get; set; } //進度百分比
    // 畫面稱為「功能介紹」，CLR 舊名稱保留；資料庫實際欄位是單數 Remark。
    [Column("Remark")]
    public string? Remarks { get; set; }
    public Works Work { get; set; } = null!;
}
