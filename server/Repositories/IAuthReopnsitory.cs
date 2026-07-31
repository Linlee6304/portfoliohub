using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories
{
    public interface IAuthReopnsitory
    {
        Task<bool> IsEmailExists(string email);//檢查郵箱是否存在
        Task<bool> IsIdentityUserIdExists(string identityUserId);//檢查identityUserId是否存在於CreatorProfiles中
        Task CreateCreatorProfile(CreatorProfiles profile);//創建創作者資料
        Task<ResponseGetAccountDto> GetAccountByEmail(string email);//用註冊信箱查詢帳號，回傳要思考修改
        Task<CreatorProfiles?> GetCreatorProfileByIdentityUserId(
    string identityUserId);

        Task SaveChangesAsync();

    }
}