using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;

namespace PortfolioHub.Server.Services;

public class WorkService(IWorkRepository repository) : IWorkService
{
    private static WorkDto Map(Works work) => new(work.WorkId, work.Title, work.Description ?? string.Empty,
        work.StarDate, work.EndDate, work.CreatedAt, work.UpdatedAt, work.WorkType, work.Status,
        work.WorkMedia.OrderBy(m => m.SortOrder).ThenBy(m => m.MediaId)
            .Select(m => new WorkMediaDto(m.MediaId, m.MediaType, m.MediaUrl, m.SortOrder, m.CreatedAt)).ToList());
    private static WorkResult<T> Missing<T>() => new(WorkResultCode.NotFound, Message: "找不到可操作的作品");
    private static WorkResult<T> NoProfile<T>() => new(WorkResultCode.MissingProfile,
        Message: "此帳戶尚無創作者資料，請先聯繫管理員建立資料後再儲存作品。");
    private static string? Validate(SaveWorkDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return "請填入作品名稱";
        if (request.Title.Length > 200 || request.Description?.Length > 5000) return "作品名稱最多 200 字，描述最多 5000 字";
        if (request.WorkType is < 1 or > 3) return "請選擇一般、接案或委託";
        if (request.Status is not (0 or 1)) return "作品狀態只能為公布或隱藏";
        if (request.Media is { } media)
        {
            if (media.Count > 50) return "媒體連結最多 50 筆";
            if (media.Any(m => m is null || m.MediaId < 0 || m.MediaType is < 1 or > 3)) return "媒體資料或類型不正確";
            var ids = media.Where(m => m.MediaId > 0).Select(m => m.MediaId).ToList();
            if (ids.Count != ids.Distinct().Count()) return "媒體編號不可重複";
            foreach (var item in media)
                if (string.IsNullOrWhiteSpace(item.MediaUrl) || item.MediaUrl.Length > 2048
                    || !Uri.TryCreate(item.MediaUrl.Trim(), UriKind.Absolute, out var url)
                    || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                    || string.IsNullOrWhiteSpace(url.Host)) return "請填入完整的 http:// 或 https:// 媒體連結（最多 2048 字）";
        }
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Value.Date)
            return "結束日期不可早於開始日期";
        return null;
    }

    private static Works Changes(SaveWorkDto request, DateTime now) => new()
    {
        Title = request.Title.Trim(), Description = request.Description?.Trim() ?? "",
        StarDate = request.StartDate?.Date, EndDate = request.EndDate?.Date,
        CreatedAt = now, UpdatedAt = now, WorkType = request.WorkType, Status = request.Status,
        WorkMedia = (request.Media ?? []).Select((m, index) => new WorkMedia
        {
            MediaId = m.MediaId, MediaType = m.MediaType, MediaUrl = m.MediaUrl.Trim(),
            SortOrder = index, CreatedAt = now
        }).ToList()
    };

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
        if (request.Media?.Any(m => m.MediaId != 0) == true)
            return new(WorkResultCode.Invalid, Message: "新增作品不能帶入既有媒體編號");
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return NoProfile<WorkDto>();
        var now = DateTime.UtcNow;
        var work = await repository.CreateAsync(creatorId.Value, Changes(request, now), ct);
        return new(WorkResultCode.Success, Map(work));
    }

    public async Task<WorkResult<WorkDto>> UpdateAsync(string userId, int workId, SaveWorkDto request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return new(WorkResultCode.Invalid, Message: error);
        var creatorId = await repository.FindCreatorIdAsync(userId, ct);
        if (creatorId is null) return Missing<WorkDto>();
        var result = await repository.UpdateAsync(creatorId.Value, workId, Changes(request, DateTime.UtcNow), request.Media is not null, ct);
        if (result.InvalidMedia) return new(WorkResultCode.Invalid, Message: "媒體不屬於此作品，請重新載入後再試");
        return result.Work is null ? Missing<WorkDto>() : new(WorkResultCode.Success, Map(result.Work));
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
