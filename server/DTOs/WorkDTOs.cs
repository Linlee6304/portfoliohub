using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Server.DTOs;

// 只接受可編輯欄位；帳戶、創作者、建立時間由伺服器決定。
public class SaveWorkDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [StringLength(5000)]
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Range(1, 3)] public int WorkType { get; set; } = 1;
    [Range(0, 1)] public int Status { get; set; } = 1;
    // null／省略表示保留既有媒體；空陣列表示刪除全部，避免舊客戶端意外清空。
    [MaxLength(50)] public List<SaveWorkMediaDto>? Media { get; set; }
}

public class SaveWorkMediaDto
{
    // 0 表示新增；大於 0 只能指向本作品既有的媒體。
    [Range(0, int.MaxValue)] public int MediaId { get; set; }
    [Range(1, 3)] public int MediaType { get; set; } = 1;
    [Required, StringLength(2048)] public string MediaUrl { get; set; } = "";
}

public record WorkMediaDto(int MediaId, int MediaType, string MediaUrl, int SortOrder, DateTime CreatedAt);

public class DeleteWorksDto
{
    [Required, MinLength(1), MaxLength(100)]
    public int[] WorkIds { get; set; } = [];
}

public record WorkDto(int WorkId, string Title, string Description, DateTime? StartDate,
    DateTime? EndDate, DateTime CreatedAt, DateTime UpdatedAt, int WorkType, int Status,
    IReadOnlyList<WorkMediaDto> Media);
public record WorkListDto(bool HasCreatorProfile, IReadOnlyList<WorkDto> Items);
public enum WorkResultCode { Success, Invalid, MissingProfile, NotFound, SharedWork }
public record WorkResult<T>(WorkResultCode Code, T? Value = default, string? Message = null);
