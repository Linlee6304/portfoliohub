namespace PortfolioHub.Server.Models.Entities;

public class WorkMedia//作品媒體表
{
    public int MediaId { get; set; }//PK,int NotNull流水號
    public int WorkId { get; set; }//FK,Works:WorkId
    public string MediaType { get; set; } = string.Empty;//媒體類型（圖片、影片、文件等）
    public string MediaUrl { get; set; } = string.Empty;//媒體URL
    public int SortOrder { get; set; } //排序順序
    public DateTime CreatedAt { get; set; } //建立時間
}