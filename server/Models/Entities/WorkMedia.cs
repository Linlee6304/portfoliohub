namespace PortfolioHub.Server.Models.Entities;

public class WorkMedia//作品媒體表
{
    public int MediaId { get; set; }//PK,int NotNull流水號
    public int WorkId { get; set; }//FK,Works:WorkId
    public int MediaType { get; set; }//對應資料庫 int：1=YOUTUBE、2=GIT、3=文件
    public string MediaUrl { get; set; } = string.Empty;//媒體URL
    public int SortOrder { get; set; } //排序順序
    public DateTime CreatedAt { get; set; } //建立時間
    public Works Work { get; set; } = null!;
}
