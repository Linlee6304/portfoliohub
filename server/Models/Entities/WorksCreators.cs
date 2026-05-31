namespace PortfolioHub.Server.Models.Entities;

public class WorksCreators//作品創作者關聯表
{
    public int WorkId { get; set; }//FK,Works:WorkId
    public int CreatorId { get; set; }//FK,CreatorProfile:CreatorId
    public string Role { get; set; } = string.Empty;//創作者在作品中的角色（如：作者、設計師、開發者等）
    public int SortOrder { get; set; } //排序順序
}