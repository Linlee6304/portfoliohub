using Microsoft.AspNetCore.Identity;

namespace PortfolioHub.Server.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public CreatorProfiles? CreatorProfile { get; set; }
}