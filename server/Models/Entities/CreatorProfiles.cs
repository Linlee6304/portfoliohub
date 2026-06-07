namespace PortfolioHub.Server.Models.Entities;

public class CreatorProfiles//創作者主表
{
    public int CreatorId { get; set; }//PK,int NotNull流水號
    public string IdentityUserId { get; set; } = string.Empty;//IdentityUser的Id
    public string DisplayName { get; set; } = string.Empty;//創作者名稱
    public string? ContactEmail { get; set; }//聯絡Email註冊時必填，修改帳號時可選填
    public string? ContactPhone { get; set; }//聯絡電話
    public string Bio { get; set; } = string.Empty;//創作者簡介
    public string? AvatarUrl { get; set; }//頭像URL
    public bool IsActive { get; set; } //啟用狀況
    public int WorkStatus { get; set; } //接案狀況 0不接案/1接案中/2可接案
    public DateTime CreatedAt { get; set; } //建立時間
    public DateTime UpdatedAt { get; set; } //最後更新
}
