using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PortfolioHub.Server.Repositories
{
    public class CreatorScheduleRepository : ICreatorScheduleRepository
    {
        private readonly AppDbContext _context;
        public CreatorScheduleRepository(AppDbContext context)
        {
            _context = context;
        }
    }
}