using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Server.DTOs;

// 此階段僅開放作品基本欄位；不接受前端指定帳戶、創作者、狀態或建立時間。
public class SaveWorkDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(5000)]
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class DeleteWorksDto
{
    [Required, MinLength(1), MaxLength(100)]
    public int[] WorkIds { get; set; } = [];
}

public record WorkDto(int WorkId, string Title, string Description, DateTime? StartDate,
    DateTime? EndDate, DateTime CreatedAt, DateTime UpdatedAt);
public record WorkListDto(bool HasCreatorProfile, IReadOnlyList<WorkDto> Items);
public enum WorkResultCode { Success, Invalid, MissingProfile, NotFound, SharedWork }
public record WorkResult<T>(WorkResultCode Code, T? Value = default, string? Message = null);
