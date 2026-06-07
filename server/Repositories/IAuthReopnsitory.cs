using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories
{
    public interface IAuthReopnsitory
    {
        Task<bool> IsEmailExists(string email);//檢查郵箱是否存在
        Task CreateCreatorProfile(CreatorProfiles profile);//創建創作者資料
    }
}