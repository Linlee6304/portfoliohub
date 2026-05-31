namespace PortfolioHub.Server.Models.Entities;

public class WorkFeatures//作品特點表
{
    public int FeatureId { get; set; }//PK,int NotNull流水號
    public int WorkId { get; set; }//FK → Works:WorkId
    public string FeatureName { get; set; } = string.Empty;//特點名稱
    public DateTime? StartDate { get; set; } //開始日期
    public DateTime? EndDate { get; set; } //結束日期
    public int Status { get; set; } //特點狀態
    public byte Progress { get; set; } //進度百分比
    public string? Remarks { get; set; } //備註 
}