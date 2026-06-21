using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Repositories;

namespace PortfolioHub.Server.Services
{
    public interface IAuthService
    {
        Task<ResponseAuthDto> Register(RequestLoginRegisterDto request);//註冊
        Task<ResponseAuthInfoDto> Login(RequestLoginRegisterDto request);//登入
        Task<ResponseAuthDto> GetAccountByEmail(string email);//用註冊信箱查詢帳號
        Task<ResponseAuthDto> UpdateAccount(RequestAuthDto request);//更新帳號


    }
}