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
        .AsNoTracking().Include(w => w.WorkMedia).Include(w => w.WorkFeatures).AsSplitQuery()
        .OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.WorkId).ToListAsync(ct);

    public Task<Works?> FindAsync(int creatorId, int workId, CancellationToken ct) => Mine(creatorId)
        .AsNoTracking().Include(w => w.WorkMedia).Include(w => w.WorkFeatures).AsSplitQuery()
        .SingleOrDefaultAsync(w => w.WorkId == workId, ct);

    public async Task<Works> CreateAsync(int creatorId, Works work, CancellationToken ct)
    {
        work.WorksCreators.Add(new WorksCreators { CreatorId = creatorId, Role = "作者", SortOrder = 0 });
        db.Works.Add(work);
        await db.SaveChangesAsync(ct);
        return work;
    }

    public async Task<WorkUpdateResult> UpdateAsync(int creatorId, int workId, Works changes,
        bool replaceMedia, bool replaceFeatures, CancellationToken ct)
    {
        var work = await Mine(creatorId).Include(w => w.WorkMedia).Include(w => w.WorkFeatures).AsSplitQuery()
            .SingleOrDefaultAsync(w => w.WorkId == workId, ct);
        if (work is null) return new(null);
        if (replaceMedia && changes.WorkMedia.Any(m => m.MediaId > 0 && !work.WorkMedia.Any(old => old.MediaId == m.MediaId)))
            return new(null, true);
        // 全部歸屬檢查通過才修改追蹤實體，防止跨作品挪用功能。
        if (replaceFeatures && changes.WorkFeatures.Any(f => f.FeatureId > 0 && !work.WorkFeatures.Any(old => old.FeatureId == f.FeatureId)))
            return new(null, InvalidFeatures: true);
        work.Title = changes.Title;
        work.Description = changes.Description;
        // CLR 舊名稱 StarDate 已映射至資料庫 StartDate。
        work.StarDate = changes.StarDate;
        work.EndDate = changes.EndDate;
        work.WorkType = changes.WorkType;
        work.Status = changes.Status;
        work.UpdatedAt = changes.UpdatedAt;
        if (replaceMedia)
        {
            foreach (var old in work.WorkMedia.ToList())
                if (!changes.WorkMedia.Any(m => m.MediaId == old.MediaId))
                {
                    db.WorkMedia.Remove(old);
                    work.WorkMedia.Remove(old);
                }
            foreach (var item in changes.WorkMedia)
            {
                if (item.MediaId == 0) work.WorkMedia.Add(item);
                else
                {
                    var old = work.WorkMedia.Single(m => m.MediaId == item.MediaId);
                    old.MediaType = item.MediaType;
                    old.MediaUrl = item.MediaUrl;
                    old.SortOrder = item.SortOrder;
                }
            }
        }
        if (replaceFeatures)
        {
            foreach (var old in work.WorkFeatures.ToList())
                if (!changes.WorkFeatures.Any(f => f.FeatureId == old.FeatureId))
                {
                    db.WorkFeatures.Remove(old);
                    work.WorkFeatures.Remove(old);
                }
            foreach (var item in changes.WorkFeatures)
            {
                if (item.FeatureId == 0) work.WorkFeatures.Add(item);
                else
                {
                    var old = work.WorkFeatures.Single(f => f.FeatureId == item.FeatureId);
                    old.FeatureName = item.FeatureName;
                    old.StartDate = item.StartDate;
                    old.EndDate = item.EndDate;
                    old.Status = item.Status;
                    old.Progress = item.Progress;
                    old.Remarks = item.Remarks;
                }
            }
        }
        // 單次 SaveChanges 交易使主表、功能與媒體全部成功或全部回滾。
        await db.SaveChangesAsync(ct);
        return new(work);
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
