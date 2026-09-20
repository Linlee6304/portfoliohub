using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using Xunit;

namespace PortfolioHub.Tests;

public class WorkTests : IDisposable
{
    private readonly string database = Path.Combine(Path.GetTempPath(), "portfoliohub-work-" + Guid.NewGuid() + ".db");
    private const string Password = "WorkTest!234";
    private static void Auth(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static async Task<ResponseAuthInfoDto> Register(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/register", new {
            email, password = Password, confirmPassword = Password, displayName = "作品測試"
        })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/Auth/login", new { email, password = Password });
        var account = (await login.Content.ReadFromJsonAsync<ResponseAuthInfoDto>())!;
        Auth(client, account.Token!);
        return account;
    }
    private static async Task<WorkDto> Create(HttpClient client, string title = "測試作品")
    {
        var response = await client.PostAsJsonAsync("/api/works", new {
            title, description = "測試描述", startDate = "2026-01-01", endDate = "2026-02-01",
            creatorId = -999, identityUserId = "someone-else", status = 1, workType = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<WorkDto>())!;
    }

    [Fact]
    public async Task AllEndpointsRequireLogin()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/works")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/works/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/works", new { title = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/works/1", new { title = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = new[] { 1 } })).StatusCode);
    }

    [Fact]
    public async Task CreateAndEditPersistFieldsAndUseAuthenticatedCreator()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var account = await Register(client, "owner@example.test");
        var empty = (await client.GetFromJsonAsync<WorkListDto>("/api/works"))!;
        Assert.True(empty.HasCreatorProfile);
        Assert.Empty(empty.Items);
        var work = await Create(client, "  首個作品  ");
        Assert.Equal("首個作品", work.Title);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = await db.Works.Include(w => w.WorksCreators).SingleAsync();
            Assert.Equal(1, saved.Status);
            Assert.Equal(1, saved.WorkType);
            var creator = await db.CreatorProfiles.SingleAsync(p => p.IdentityUserId == account.IdentityUserId);
            Assert.Equal(creator.CreatorId, Assert.Single(saved.WorksCreators).CreatorId);
            saved.Status = 7; saved.WorkType = 2;
            db.WorkFeatures.Add(new WorkFeatures { WorkId = work.WorkId, FeatureName = "既有功能" });
            await db.SaveChangesAsync();
        }
        var response = await client.PutAsJsonAsync($"/api/works/{work.WorkId}", new {
            title = "更新後", description = "  新描述  ", startDate = (string?)null, endDate = (string?)null, status = 0, workType = 3
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{work.WorkId}"))!;
        Assert.Equal("更新後", result.Title);
        Assert.Equal("新描述", result.Description);
        Assert.Null(result.StartDate);
        Assert.Equal(work.CreatedAt, result.CreatedAt);
        using var verify = factory.Services.CreateScope();
        var context = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var record = await context.Works.SingleAsync();
        Assert.Equal(0, record.Status);
        Assert.Equal(3, record.WorkType);
        Assert.Single(await context.WorkFeatures.ToListAsync());
    }

    [Fact]
    public async Task OtherCreatorsCannotReadUpdateOrPartiallyDeleteWorks()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var first = await Register(client, "first@example.test");
        var firstWork = await Create(client);
        await Register(client, "second@example.test");
        var secondWork = await Create(client);
        Assert.Equal(secondWork.WorkId, Assert.Single((await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items).WorkId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/works/{firstWork.WorkId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/works/{firstWork.WorkId}", new { title = "hacked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = new[] { firstWork.WorkId, secondWork.WorkId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/works/{secondWork.WorkId}")).StatusCode);
        Auth(client, first.Token!);
        Assert.Equal("測試作品", (await client.GetFromJsonAsync<WorkDto>($"/api/works/{firstWork.WorkId}"))!.Title);
    }

    [Fact]
    public async Task InvalidInputDoesNotCreateOrOverwriteData()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "validation@example.test");
        foreach (var input in new object[] {
            new { title = "   " }, new { title = new string('x', 201) },
            new { title = "x", description = new string('x', 5001) },
            new { title = "x", startDate = "2026-02-01", endDate = "2026-01-01" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works", input)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items);
        var work = await Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{work.WorkId}", new { title = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = Array.Empty<int>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = new[] { -1 } })).StatusCode);
        Assert.Equal("測試作品", (await client.GetFromJsonAsync<WorkDto>($"/api/works/{work.WorkId}"))!.Title);
    }

    [Fact]
    public async Task AdminWithoutProfileGetsEmptyStateAndCannotCreateImplicitProfile()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var account = await Register(client, "admin-empty@example.test");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Roles.SingleAsync(r => r.Name == "Admin");
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = account.IdentityUserId!, RoleId = admin.Id });
            db.CreatorProfiles.Remove(await db.CreatorProfiles.SingleAsync());
            await db.SaveChangesAsync();
        }
        var list = (await client.GetFromJsonAsync<WorkListDto>("/api/works"))!;
        Assert.False(list.HasCreatorProfile);
        Assert.Empty(list.Items);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/works", new { title = "新作品" })).StatusCode);
        using var verify = factory.Services.CreateScope();
        var context = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await context.CreatorProfiles.ToListAsync());
        Assert.Empty(await context.Works.ToListAsync());
    }

    [Fact]
    public async Task SharedWorksBlockWholeBatchAndOwnWorksCascadeDelete()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var owner = await Register(client, "owner@example.test");
        var own = await Create(client, "獨立作品");
        var shared = await Create(client, "共同作品");
        var other = await Register(client, "other@example.test");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherId = await db.CreatorProfiles.Where(p => p.IdentityUserId == other.IdentityUserId).Select(p => p.CreatorId).SingleAsync();
            db.WorksCreators.Add(new WorksCreators { WorkId = shared.WorkId, CreatorId = otherId, Role = "設計師" });
            db.WorkMedia.Add(new WorkMedia { WorkId = own.WorkId, MediaType = 1, MediaUrl = "https://example.test/image.png" });
            db.WorkFeatures.Add(new WorkFeatures { WorkId = own.WorkId, FeatureName = "附屬功能" });
            await db.SaveChangesAsync();
        }
        Auth(client, owner.Token!);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = new[] { own.WorkId, shared.WorkId } })).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items.Count);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/works/delete-batch", new { workIds = new[] { own.WorkId, own.WorkId } })).StatusCode);
        using var verify = factory.Services.CreateScope();
        var context = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(shared.WorkId, (await context.Works.SingleAsync()).WorkId);
        Assert.Empty(await context.WorkMedia.ToListAsync());
        Assert.Empty(await context.WorkFeatures.ToListAsync());
        Assert.False(await context.WorksCreators.AnyAsync(c => c.WorkId == own.WorkId));
    }

    [Fact]
    public async Task MediaRoundTripPreservesIdsAndCreationTimeAndSupportsExplicitClear()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "media@example.test");
        var response = await client.PostAsJsonAsync("/api/works", new {
            title = "媒體作品", workType = 2, status = 0,
            media = new[] {
                new { mediaType = 1, mediaUrl = "https://example.test/video" },
                new { mediaType = 2, mediaUrl = "https://example.test/code" },
                new { mediaType = 3, mediaUrl = "https://example.test/document" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var original = (await response.Content.ReadFromJsonAsync<WorkDto>())!;
        Assert.Equal(2, original.WorkType); Assert.Equal(0, original.Status);
        Assert.Equal(new[] { 0, 1, 2 }, original.Media.Select(m => m.SortOrder));
        var retained = original.Media[2];
        response = await client.PutAsJsonAsync($"/api/works/{original.WorkId}", new {
            title = "更新媒體", workType = 3, status = 1,
            media = new[] {
                new { mediaId = retained.MediaId, mediaType = 2, mediaUrl = "  https://example.test/changed  " },
                new { mediaId = 0, mediaType = 3, mediaUrl = "https://example.test/new" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Equal(2, saved.Media.Count); Assert.Equal(3, saved.WorkType); Assert.Equal(1, saved.Status);
        Assert.Equal(retained.MediaId, saved.Media[0].MediaId);
        Assert.Equal(retained.CreatedAt, saved.Media[0].CreatedAt);
        Assert.Equal("https://example.test/changed", saved.Media[0].MediaUrl);
        Assert.DoesNotContain(saved.Media, m => m.MediaId == original.Media[0].MediaId);
        // 舊客戶端省略媒體不清空；明確送 [] 才刪除全部。
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/works/{original.WorkId}", new { title = "保留媒體" })).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!.Media.Count);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/works/{original.WorkId}", new { title = "清空", media = Array.Empty<object>() })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!.Media);
    }

    [Fact]
    public async Task InvalidMediaOrForeignMediaCannotPartiallyOverwriteWorks()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "media-validation@example.test");
        var work = await Create(client);
        var sourceResponse = await client.PostAsJsonAsync("/api/works", new { title = "另一作品",
            media = new[] { new { mediaType = 1, mediaUrl = "https://example.test/video" } } });
        var source = (await sourceResponse.Content.ReadFromJsonAsync<WorkDto>())!;
        var foreignId = source.Media[0].MediaId;
        foreach (var input in new object[] {
            new { title = "修改", workType = 0 }, new { title = "修改", status = 2 },
            new { title = "修改", media = new[] { new { mediaType = 4, mediaUrl = "https://example.test" } } },
            new { title = "修改", media = new[] { new { mediaType = 1, mediaUrl = "javascript:alert(1)" } } },
            new { title = "修改", media = new[] { new { mediaType = 1, mediaUrl = "" } } },
            new { title = "修改", media = new[] { new { mediaType = 1, mediaUrl = "file:///C:/test" } } },
            new { title = "修改", media = new[] { new { mediaId = foreignId, mediaType = 1, mediaUrl = "https://example.test/hijack" } } },
            new { title = "修改", media = new object?[] { null } },
            new { title = "修改", media = Enumerable.Range(0, 51).Select(_ => new { mediaType = 1, mediaUrl = "https://example.test" }).ToArray() }
        })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{work.WorkId}", input)).StatusCode);
            Assert.Equal(work.Title, (await client.GetFromJsonAsync<WorkDto>($"/api/works/{work.WorkId}"))!.Title);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{source.WorkId}", new {
            title = "重複", media = new[] {
                new { mediaId = foreignId, mediaType = 1, mediaUrl = "https://example.test/a" },
                new { mediaId = foreignId, mediaType = 2, mediaUrl = "https://example.test/b" }
            }
        })).StatusCode);
        Assert.Equal("https://example.test/video", (await client.GetFromJsonAsync<WorkDto>($"/api/works/{source.WorkId}"))!.Media[0].MediaUrl);
        // 不同帳戶也不能帶入其他作品的媒體 ID。
        await Register(client, "media-other@example.test");
        var other = await Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/works/{other.WorkId}", new {
            title = "竄改", media = new[] { new { mediaId = foreignId, mediaType = 1, mediaUrl = "https://example.test/hijack" } }
        })).StatusCode);
    }

    [Fact]
    public async Task DatabaseFailureRollsBackWorkAndMediaTogether()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        await Register(client, "atomic@example.test");
        var original = await Create(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailMedia BEFORE INSERT ON WorkMedia
                BEGIN SELECT RAISE(ABORT, 'test insert failure'); END;
                """);
        }
        // 直接驗證 Repository 的交易；不讓測試主機的 Windows EventLog 權限干擾預期錯誤。
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=" + database).Options))
        {
            var creatorId = await db.CreatorProfiles.Select(p => p.CreatorId).SingleAsync();
            var repository = new PortfolioHub.Server.Repositories.WorkRepository(db);
            var change = new Works { Title = "不可部分儲存", WorkType = 3, Status = 0, UpdatedAt = DateTime.UtcNow,
                WorkMedia = new List<WorkMedia> { new() { MediaType = 1, MediaUrl = "https://example.test/video", CreatedAt = DateTime.UtcNow } } };
            await Assert.ThrowsAsync<DbUpdateException>(() => repository.UpdateAsync(creatorId, original.WorkId, change, true, CancellationToken.None));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateAsync(creatorId, change, CancellationToken.None));
        }
        var saved = (await client.GetFromJsonAsync<WorkDto>($"/api/works/{original.WorkId}"))!;
        Assert.Equal(original.Title, saved.Title); Assert.Equal(original.WorkType, saved.WorkType);
        Assert.Equal(original.UpdatedAt, saved.UpdatedAt); Assert.Empty(saved.Media);
        Assert.Single((await client.GetFromJsonAsync<WorkListDto>("/api/works"))!.Items);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
