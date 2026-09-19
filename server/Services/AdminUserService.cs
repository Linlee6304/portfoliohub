using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Repositories;
namespace PortfolioHub.Server.Services;
public class AdminUserService(IAdminUserRepository repository) : IAdminUserService
{
    public Task<bool> CanManage(string userId) => repository.IsAdmin(userId);
    public async Task<List<AdminUserDto>> ListUsers() => (await repository.ListUsers()).Select(p => new AdminUserDto
    {
        IdentityUserId = p.IdentityUserId, DisplayName = p.DisplayName,
        Email = p.IdentityUser.Email, ContactPhone = p.ContactPhone, IsActive = p.IsActive
    }).ToList();
    public Task<bool> SetActive(string actorId, string targetId, bool isActive) =>
        actorId == targetId ? Task.FromResult(false) : repository.SetActive(targetId, isActive);
}
