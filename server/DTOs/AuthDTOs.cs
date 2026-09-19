using System.ComponentModel.DataAnnotations;
namespace PortfolioHub.Server.DTOs;

public class RequestLoginRegisterDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }
    [StringLength(100)]
    public string? DisplayName { get; set; }
    [StringLength(30)]
    public string? ContactPhone { get; set; }
}
// 白名單 DTO：登入信箱、IdentityUserId、角色、接案狀態都不在此更新契約中。
public class RequestUpdateProfileDto
{
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;
    [StringLength(30)]
    public string? ContactPhone { get; set; }
    [StringLength(2048)]
    public string? AvatarUrl { get; set; }
    [StringLength(2000)]
    public string? Bio { get; set; }
}
public class RequestWorkStatusDto
{
    [Required, Range(0, 2)]
    public int? WorkStatus { get; set; }
}
public class ResponseWorkStatusDto : ResponseAuthDto
{
    public int? WorkStatus { get; set; }
}
public class ResponseAuthDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
public class ResponseAuthInfoDto : ResponseAuthDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Role { get; set; }
    public string? IdentityUserId { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public string? Email { get; set; }
    public int? WorkStatus { get; set; }
}
public class ResponseGetAccountDto : ResponseAuthDto
{
    public string? IdentityUserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AvatarUrl { get; set; }
    public int? WorkStatus { get; set; }
    public string? Bio { get; set; }
    public string? Role { get; set; }
}
public class RequestChangePasswordDto
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
    [Required, Compare(nameof(NewPassword), ErrorMessage = "新密碼與確認密碼不一致")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
public class ResponseChangePasswordDto : ResponseAuthDto
{
    public IEnumerable<string>? Errors { get; set; }
}
