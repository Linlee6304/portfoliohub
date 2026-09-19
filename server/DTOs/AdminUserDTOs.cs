using System.ComponentModel.DataAnnotations;
namespace PortfolioHub.Server.DTOs;

public class AdminUserDto
{
    public string IdentityUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}
public class RequestAccountStatusDto
{
    [Required]
    public bool? IsActive { get; set; }
}
