using System.Data;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Repositories;

public class WorkRepository(AppDbContext db) : IWorkRepository
{
    public Task<int?> FindCreatorIdAsync(string userId, CancellationToken ct) => db.CreatorProfiles
        .Where(p => p.IdentityUserId == userId && p.IsActive)
        .Select(p => (int?)p.CreatorId).SingleOrDefaultAsync(ct);

    private IQueryable<Works> Mine(int creatorId) => db.Works
        .Where(w => w.WorksCreators.Any(c => c.CreatorId == creatorId));

    public Task<List<Works>> ListAsync(int creatorId, CancellationToken ct) => Mine(creatorId)
        .AsNoTracking().OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.WorkId).ToListAsync(ct);

    public Task<Works?> FindAsync(int creatorId, int workId, CancellationToken ct) => Mine(creatorId)
        .AsNoTracking().SingleOrDefaultAsync(w => w.WorkId == workId, ct);

    public async Task<Works> CreateAsync(int creatorId, Works work, CancellationToken ct)
    {
        work.WorksCreators.Add(new WorksCreators { CreatorId = creatorId, Role = "作者", SortOrder = 0 });
        db.Works.Add(work);
        await db.SaveChangesAsync(ct);
        return work;
    }

    public async Task<Works?> UpdateAsync(int creatorId, int workId, string title, string description,
        DateTime? startDate, DateTime? endDate, DateTime updatedAt, CancellationToken ct)
    {
        var work = await Mine(creatorId).SingleOrDefaultAsync(w => w.WorkId == workId, ct);
        if (work is null) return null;
        work.Title = title;
        work.Description = description;
        // 沿用資料庫既有 StarDate 拼字；對外 DTO 使用 StartDate。
        work.StarDate = startDate;
        work.EndDate = endDate;
        work.UpdatedAt = updatedAt;
        await db.SaveChangesAsync(ct);
        return work;
    }

    public async Task<WorkWriteResult> DeleteAsync(int creatorId, int[] workIds, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var works = await Mine(creatorId).Where(w => workIds.Contains(w.WorkId))
            .Include(w => w.WorksCreators).ToListAsync(ct);
        if (works.Count != workIds.Length) return WorkWriteResult.NotFound;
        if (works.Any(w => w.WorksCreators.Count > 1)) return WorkWriteResult.SharedWork;
        db.Works.RemoveRange(works);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return WorkWriteResult.Success;
    }
}
