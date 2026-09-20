using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;

namespace PortfolioHub.Server.Services;

public class WorkService(IWorkRepository repository) : IWorkService
{
    private static WorkDto Map(Works work) => new(work.WorkId, work.Title, work.Description ?? string.Empty,
        work.StarDate, work.EndDate, work.CreatedAt, work.UpdatedAt);
    private static WorkResult<T> Missing<T>() => new(WorkResultCode.NotFound, Message: "找不到可操作的作品");
    private static WorkResult<T> NoProfile<T>() => new(WorkResultCode.MissingProfile,
        Message: "此帳戶尚無創作者資料，請先聯繫管理員建立資料後再儲存作品。");
    private static string? Validate(SaveWorkDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return "請填入作品名稱";
        if (request.Title.Length > 200 || request.Description?.Length > 5000) return "作品名稱最多 200 字，描述最多 5000 字";
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Value.Date)
            return "結束日期不可早於開始日期";
        return null;
    }

    public async Task<WorkListDto> ListAsync(string userId, CancellationToken ct)
    {
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        return creatorId is null ? new(false, []) : new(true,
            (await repository.ListAsync(creatorId.Value, ct)).Select(Map).ToList());
    }

    public async Task<WorkResult<WorkDto>> GetAsync(string userId, int workId, CancellationToken ct)
    {
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return Missing<WorkDto>();
        var work = await repository.FindAsync(creatorId.Value, workId, ct);
        return work is null ? Missing<WorkDto>() : new(WorkResultCode.Success, Map(work));
    }

    public async Task<WorkResult<WorkDto>> CreateAsync(string userId, SaveWorkDto request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return new(WorkResultCode.Invalid, Message: error);
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return NoProfile<WorkDto>();
        var now = DateTime.UtcNow;
        var work = await repository.CreateAsync(creatorId.Value, new Works
        {
            Title = request.Title.Trim(), Description = request.Description?.Trim() ?? "",
            StarDate = request.StartDate?.Date, EndDate = request.EndDate?.Date,
            CreatedAt = now, UpdatedAt = now,
            // 狀態／類型尚未開放編輯，沿用既有整數預設值，不新增狀態語意。
            Status = 0, WorkType = 0
        }, ct);
        return new(WorkResultCode.Success, Map(work));
    }

    public async Task<WorkResult<WorkDto>> UpdateAsync(string userId, int workId, SaveWorkDto request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return new(WorkResultCode.Invalid, Message: error);
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return Missing<WorkDto>();
        var work = await repository.UpdateAsync(creatorId.Value, workId, request.Title.Trim(),
            request.Description?.Trim() ?? "", request.StartDate?.Date, request.EndDate?.Date, DateTime.UtcNow, ct);
        return work is null ? Missing<WorkDto>() : new(WorkResultCode.Success, Map(work));
    }

    public async Task<WorkResult<bool>> DeleteAsync(string userId, DeleteWorksDto request, CancellationToken ct)
    {
        if (request.WorkIds is null || request.WorkIds.Length is < 1 or > 100 || request.WorkIds.Any(id => id <= 0))
            return new(WorkResultCode.Invalid, Message: "請選取 1 至 100 筆有效作品");
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return Missing<bool>();
        var result = await repository.DeleteAsync(creatorId.Value, request.WorkIds.Distinct().ToArray(), ct);
        return result switch
        {
            WorkWriteResult.NotFound => Missing<bool>(),
            WorkWriteResult.SharedWork => new(WorkResultCode.SharedWork, Message: "所選作品含其他共同創作者，本次未刪除任何作品。"),
            _ => new(WorkResultCode.Success, true)
        };
    }
}
