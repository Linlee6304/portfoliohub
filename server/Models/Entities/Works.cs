namespace PortfolioHub.Server.Models.Entities;

public class Works//作品主表
{
    public int WorkId { get; set; }//PK,int NotNull流水號
    public string Title { get; set; } = string.Empty;//作品名稱
    public string Description { get; set; } = string.Empty;//作品描述
    public DateTime? StarDate { get; set; } //開始日期
    public DateTime? EndDate { get; set; } //結束日期
    public int Status { get; set; } //作品狀態
    public DateTime CreatedAt { get; set; } //建立時間
    public DateTime UpdatedAt { get; set; } //最後更新
    public int WorkType { get; set; } //作品類型 一般/接案/委託

    public ICollection<WorkFeatures> WorkFeatures { get; set; }
        = new List<WorkFeatures>();

    public ICollection<WorkMedia> WorkMedia { get; set; }
        = new List<WorkMedia>();

    public ICollection<WorksCreators> WorksCreators { get; set; }
        = new List<WorksCreators>();

}