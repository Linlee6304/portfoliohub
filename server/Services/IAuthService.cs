using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Repositories;

namespace PortfolioHub.Server.Services
{
    public interface IAuthService
    {
        Task<ResponseAuthDto> Register(RequestLoginRegisterDto request);//註冊
        Task<ResponseAuthDto> Login(RequestLoginRegisterDto request);//登入
        Task<ResponseAuthDto> UpdateAccount(RequestAuthDto request);//修改帳號
    }
}