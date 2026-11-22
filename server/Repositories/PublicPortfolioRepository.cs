using System.Data;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories
{
    public class PublicPortfolioRepository(AppDbContext db) : IPublicPortfolioRepository
    {
        //集中前台的資料庫查詢
    }
}