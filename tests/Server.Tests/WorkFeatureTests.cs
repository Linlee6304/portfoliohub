using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
using Xunit;

namespace PortfolioHub.Tests;

public partial class WorkTests
{
    private static SaveWorkFeatureDto Feature(string name = "功能", int progress = 0, int status = 0) =>
        new() { FeatureName = name, Progress = progress, Status = status };

    [Fact]
    public async Task FeaturesRoundTripAllStatusesOptionalDatesAndMediaWithoutLosingIds()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "features@example.test");
        var input = new SaveWorkDto {
            Title = "完整作品", Features = Enumerable.Range(0, 5).Select(s => Feature("功能" + s, s == 1 ? 100 : 65, s)).ToList(),
            Media = [new() { MediaType = 2, MediaUrl = "https://example.test/code" }]
        };
        // 完成狀態和進度獨立：不強制 100%，也不自動改寫。
        input.Features[1].Progress = 80;
        input.Features[2].StartDate = new DateTime(2026, 8, 1);
        input.Features[3].EndDate = new DateTime(2026, 8, 20);
        input.Features[0].Introduction = "  使用三層式架構製作  ";
        var response = await client.PostAsJsonAsync("/api/works", input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var original = (await response.Content.ReadFromJsonAsync<WorkDto>())!;
        var detail = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, detail.Features.Select(f => f.Status));
        Assert.Equal(80, detail.Features[1].Progress);
        Assert.Null(detail.Features[0].StartDate); Assert.Null(detail.Features[0].EndDate);
        Assert.Null(detail.Features[2].EndDate); Assert.Null(detail.Features[3].StartDate);
        Assert.Equal("使用三層式架構製作", detail.Features[0].Introduction);
        Assert.Equal("", detail.Features[1].Introduction);
        var existingId = detail.Features[0].FeatureId;
        input.Features = [
            new() { FeatureId = existingId, FeatureName = "修改後功能", Progress = 100, Status = 1,
                StartDate = new DateTime(2026, 8, 1), EndDate = new DateTime(2026, 8, 20), Introduction = "新的介紹" },
            Feature("新增功能")
        ];
        input.Media = [new() { MediaId = detail.Media[0].MediaId, MediaType = 3, MediaUrl = "https://example.test/docs" }];
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/works/{original.WorkId}", input)).StatusCode);
        detail = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Equal(2, detail.Features.Count);
        Assert.Equal(existingId, detail.Features[0].FeatureId);
        Assert.Equal("新的介紹", detail.Features[0].Introduction);
        Assert.DoesNotContain(detail.Features, f => f.FeatureId == original.Features[1].FeatureId);
        Assert.Equal(original.Media[0].MediaId, Assert.Single(detail.Media).MediaId);
        Assert.Equal(original.Media[0].CreatedAt, detail.Media[0].CreatedAt);
        Assert.Equal("https://example.test/docs", detail.Media[0].MediaUrl);
        // 舊版未送 Features 時保留，明確 [] 清空；媒體仍保留。
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/works/{original.WorkId}", new { title = "僅主表" })).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!.Features.Count);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/works/{original.WorkId}", new { title = "清空功能", features = Array.Empty<object>() })).StatusCode);
        detail = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Empty(detail.Features); Assert.Single(detail.Media);
    }

    [Fact]
    public async Task InvalidFeatureInputRejectsWholeSaveIncludingMediaChanges()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "feature-validation@example.test");
        var work = await Create(client);
        var invalid = new object[] {
            Feature(" "), Feature(new string('x', 201)), Feature(progress: -1), Feature(progress: 101),
            Feature(status: -1), Feature(status: 5),
            new { featureName = "小數進度", progress = 0.5 },
            new SaveWorkFeatureDto { FeatureName = "超長介紹", Introduction = new string('x', 5001) },
            new SaveWorkFeatureDto { FeatureName = "錯誤日期", StartDate = new DateTime(2026, 9, 2), EndDate = new DateTime(2026, 9, 1) },
            new SaveWorkFeatureDto { FeatureName = "負值ID", FeatureId = -1 }
        };
        foreach (var item in invalid)
        {
            var response = await client.PutAsJsonAsync($"/api/works/{work.WorkId}", new { title = "不得寫入",
                features = new[] { item }, media = new[] { new { mediaType = 1, mediaUrl = "https://example.test/new" } } });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works",
            new { title = "空項目", features = new object?[] { null } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works",
            new { title = "超過上限", features = Enumerable.Range(0, 51).Select(_ => Feature()).ToArray() })).StatusCode);
        var saved = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{work.WorkId}"))!;
        Assert.Equal(work.Title, saved.Title); Assert.Empty(saved.Media); Assert.Empty(saved.Features);
        Assert.Single((await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items);
    }

    [Fact]
    public async Task ForeignAndDuplicateFeatureIdsCannotBeReassignedOrPartiallySaved()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var owner = await Register(client, "feature-owner@example.test");
        var response = await client.PostAsJsonAsync("/api/works", new SaveWorkDto { Title = "來源作品", Features = [Feature()] });
        var source = (await response.Content.ReadFromJsonAsync<WorkDto>())!;
        var featureId = source.Features[0].FeatureId;
        var own = await Create(client);
        var invalidInput = new SaveWorkDto { Title = "不得更新", Features = [new() { FeatureId = featureId, FeatureName = "挪用" }],
            Media = [new() { MediaType = 1, MediaUrl = "https://example.test/new" }] };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{own.WorkId}", invalidInput)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works", invalidInput)).StatusCode);
        var repeated = new SaveWorkDto { Title = "重複", Features = [
            new() { FeatureId = featureId, FeatureName = "一" }, new() { FeatureId = featureId, FeatureName = "二" }] };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{source.WorkId}", repeated)).StatusCode);
        await Register(client, "feature-other@example.test");
        var other = await Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{other.WorkId}", invalidInput)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/works/{source.WorkId}", invalidInput)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/works/{source.WorkId}")).StatusCode);
        Auth(client, owner.Token!);
        var saved = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{own.WorkId}"))!;
        Assert.Equal(own.Title, saved.Title); Assert.Empty(saved.Media);
        Assert.Equal("功能", (await client.GetFromJsonAsync<WorkDto>($"/api/works/{source.WorkId}"))!.Features[0].FeatureName);
    }

    [Fact]
    public async Task FeatureDatabaseFailureRollsBackMainFeatureAndMediaUpdates()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "feature-atomic@example.test");
        var response = await client.PostAsJsonAsync("/api/works", new SaveWorkDto { Title = "原本作品",
            Features = [Feature()], Media = [new() { MediaType = 2, MediaUrl = "https://example.test/original" }] });
        var original = (await response.Content.ReadFromJsonAsync<WorkDto>())!;
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=" + database).Options))
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailFeatures BEFORE INSERT ON WorkFeatures
                BEGIN SELECT RAISE(ABORT, 'test feature failure'); END;
                """);
            var creatorId = await db.CreatorProfiles.Select(p => p.CreatorId).SingleAsync();
            var changes = new Works { Title = "未完成的更新", WorkType = 3, Status = 0, UpdatedAt = DateTime.UtcNow,
                WorkFeatures = new List<WorkFeatures> { new() { FeatureName = "新增會失敗" } },
                WorkMedia = new List<WorkMedia> { new() { MediaId = original.Media[0].MediaId, MediaType = 3, MediaUrl = "https://example.test/changed" } } };
            var repo = new WorkRepository(db);
            await Assert.ThrowsAsync<DbUpdateException>(() => repo.UpdateAsync(creatorId, original.WorkId, changes, true, true, CancellationToken.None));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<DbUpdateException>(() => repo.CreateAsync(creatorId, new Works { Title = "新增會失敗",
                WorkFeatures = new List<WorkFeatures> { new() { FeatureName = "錯誤功能" } },
                WorkMedia = new List<WorkMedia> { new() { MediaType = 1, MediaUrl = "https://example.test/new" } } }, CancellationToken.None));
        }
        var saved = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Equal(original.Title, saved.Title); Assert.Equal(original.UpdatedAt, saved.UpdatedAt);
        Assert.Equal(original.Features[0], Assert.Single(saved.Features));
        Assert.Equal(original.Media[0], Assert.Single(saved.Media));
        Assert.Single((await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items);
    }
}
