using System.Net;
using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.Models.Entities;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.DTOs;
using Xunit;

namespace PortfolioHub.Tests;

public class TestServer(string database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "Test-only-key-with-at-least-64-characters-do-not-use-in-production-12345",
            ["Jwt:Issuer"] = "PortfolioHub.Tests",
            ["Jwt:Audience"] = "PortfolioHub.Tests",
            ["Admin:Email"] = "admin@example.test",
            ["Admin:Password"] = "TestAdmin!23456"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=" + database));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }
    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });
}

public class AuthFlowTests : IDisposable
{
    private readonly string database = Path.Combine(Path.GetTempPath(), "portfoliohub-test-" + Guid.NewGuid() + ".db");
    private const string Password = "TestCreator!234";
    private TestServer Factory() => new(database);
    private static void Authorize(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static async Task<ResponseAuthInfoDto> RegisterAndLogin(HttpClient client, string email = "creator@example.test")
    {
        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            email, password = Password, confirmPassword = Password, displayName = "測試創作者"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Login(client, email);
    }
    private static async Task<ResponseAuthInfoDto> Login(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ResponseAuthInfoDto>())!;
        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        return result;
    }

    [Fact]
    public async Task CreatorAndAdminLoginReturnUsableTokens()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var creator = await RegisterAndLogin(client);
        Authorize(client, creator.Token!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Auth/me")).StatusCode);
        Assert.Equal("Creator", creator.Role);
        // 管理員須明確由資料庫配置；應用啟動不再建立或提升帳戶。
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Users.AnyAsync(u => u.Email == "admin@example.test"));
            var adminRole = await db.Roles.SingleAsync(r => r.Name == "Admin");
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = creator.IdentityUserId!, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
        var admin = await Login(client, "creator@example.test");
        Authorize(client, admin.Token!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Auth/me")).StatusCode);
        Assert.Equal("Admin", admin.Role);
    }

    [Fact]
    public async Task LogoutRevokesOnlyCurrentTokenAndSurvivesRestart()
    {
        string first, second;
        await using (var factory = Factory())
        {
            using var client = factory.Client();
            first = (await RegisterAndLogin(client)).Token!;
            second = (await Login(client, "creator@example.test")).Token!;
            Authorize(client, first);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/Auth/logout", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/Auth/logout", null)).StatusCode);
            Authorize(client, second);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Auth/me")).StatusCode);
        }
        await using var restarted = Factory();
        using var afterRestart = restarted.Client();
        Authorize(afterRestart, first);
        Assert.Equal(HttpStatusCode.Unauthorized, (await afterRestart.GetAsync("/api/Auth/me")).StatusCode);
        Authorize(afterRestart, second);
        Assert.Equal(HttpStatusCode.OK, (await afterRestart.GetAsync("/api/Auth/me")).StatusCode);
    }

    [Fact]
    public async Task ProtectedRoutesRejectMissingTokensAndWrongPassword()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/Auth/logout", null)).StatusCode);
        await RegisterAndLogin(client);
        var wrong = await client.PostAsJsonAsync("/api/Auth/login", new { email = "creator@example.test", password = "Wrong!123" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task ProfileOnlyUpdatesFourFieldsAndUsesAuthenticatedOwner()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var first = await RegisterAndLogin(client);
        var second = await RegisterAndLogin(client, "other@example.test");
        Authorize(client, first.Token!);
        var update = await client.PutAsJsonAsync("/api/Auth/profile", new
        {
            identityUserId = second.IdentityUserId, email = "changed@example.test",
            displayName = "新的名字", workStatus = 2, bio = "更新介紹",
            role = "Admin", userName = "hacked", contactPhone = "0912345678", avatarUrl = "https://example.test/avatar.png"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var me = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal(first.IdentityUserId, me!.IdentityUserId);
        Assert.Equal("新的名字", me.DisplayName);
        Assert.Equal("creator@example.test", me.Email);
        Assert.Equal("creator@example.test", me.ContactEmail);
        Assert.Equal("Creator", me.Role);
        Assert.Equal(0, me.WorkStatus);
        Assert.Equal("更新介紹", me.Bio);
        Assert.Equal("0912345678", me.ContactPhone);
        Assert.Equal("https://example.test/avatar.png", me.AvatarUrl);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal("creator@example.test", (await db.Users.SingleAsync(u => u.Id == first.IdentityUserId)).UserName);
        }
        Authorize(client, second.Token!);
        me = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal("other@example.test", me!.Email);
        Assert.Equal("測試創作者", me.DisplayName);
        await Login(client, "creator@example.test");
    }

    [Fact]
    public async Task PasswordChangeInvalidatesEveryOldSession()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var first = await RegisterAndLogin(client);
        var second = await Login(client, "creator@example.test");
        Authorize(client, first.Token!);
        var change = await client.PostAsJsonAsync("/api/Auth/change-password", new
        {
            currentPassword = Password, newPassword = "NewSecret!789", confirmNewPassword = "NewSecret!789"
        });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Auth/me")).StatusCode);
        Authorize(client, second.Token!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Auth/me")).StatusCode);
        await Login(client, "creator@example.test", "NewSecret!789");
    }

    [Fact]
    public async Task InvalidProfileFieldsAreRejectedWithoutSaving()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var first = await RegisterAndLogin(client);
        await RegisterAndLogin(client, "other@example.test");
        Authorize(client, first.Token!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/Auth/profile", new
        { email = "other@example.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/Auth/profile", new
        { displayName = "不應儲存", avatarUrl = "javascript:alert(1)" })).StatusCode);
        var account = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal("creator@example.test", account!.Email);
    }

    [Fact]
    public async Task WorkStatusIsSeparateValidatesRangeAndPreservesProfile()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsJsonAsync("/api/Auth/work-status", new { workStatus = 2 })).StatusCode);
        var first = await RegisterAndLogin(client);
        var other = await RegisterAndLogin(client, "other@example.test");
        Authorize(client, first.Token!);
        foreach (var value in new[] { 1, 2, 0 })
        {
            var response = await client.PatchAsJsonAsync("/api/Auth/work-status", new {
                workStatus = value, identityUserId = other.IdentityUserId, displayName = "不應變更", role = "Admin"
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var account = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
            Assert.Equal(value, account!.WorkStatus);
            Assert.Equal("測試創作者", account.DisplayName);
            Assert.Equal("Creator", account.Role);
        }
        foreach (var value in new[] { -1, 3 })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync("/api/Auth/work-status", new { workStatus = value })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync("/api/Auth/work-status", new { })).StatusCode);
        await client.PatchAsJsonAsync("/api/Auth/work-status", new { workStatus = 2 });
        await client.PutAsJsonAsync("/api/Auth/profile", new { displayName = "更新暱稱", workStatus = 0 });
        var saved = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal(2, saved!.WorkStatus);
        Authorize(client, other.Token!);
        Assert.Equal(0, (await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me"))!.WorkStatus);
    }

    [Fact]
    public async Task RegistrationCannotChooseAdminRoleAndStartupCannotPromoteExistingUser()
    {
        await using (var factory = Factory())
        {
            using var client = factory.Client();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/register", new {
                email = "admin@example.test", password = Password, confirmPassword = Password,
                displayName = "一般使用者", role = "Admin", roles = new[] { "Admin" }
            })).StatusCode);
            Assert.Equal("Creator", (await Login(client, "admin@example.test")).Role);
        }
        await using var restarted = Factory();
        using var afterRestart = restarted.Client();
        Assert.Equal("Creator", (await Login(afterRestart, "admin@example.test")).Role);
    }

    [Fact]
    public async Task InactiveProfileCannotBeUpdatedByExistingToken()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var account = await RegisterAndLogin(client);
        Authorize(client, account.Token!);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.CreatorProfiles.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/Auth/profile", new { displayName = "停用帳戶" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsJsonAsync("/api/Auth/work-status", new { workStatus = 2 })).StatusCode);
    }
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
