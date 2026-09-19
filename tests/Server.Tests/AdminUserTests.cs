using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.DTOs;
using Xunit;
namespace PortfolioHub.Tests;

public class AdminUserTests : IDisposable
{
    private readonly string database = Path.Combine(Path.GetTempPath(), "portfoliohub-admin-" + Guid.NewGuid() + ".db");
    private const string Password = "TestCreator!234";
    private static void Auth(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static async Task<ResponseAuthInfoDto> Register(HttpClient client, string email)
    {
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/Auth/register", new {
            email, password = Password, confirmPassword = Password, displayName = email.Split('@')[0], contactPhone = "0912345678"
        })).StatusCode);
        return await Login(client, email);
    }
    private static async Task<ResponseAuthInfoDto> Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ResponseAuthInfoDto>())!;
    }
    private static async Task<ResponseAuthInfoDto> MakeAdmin(TestServer factory, HttpClient client, string email = "admin@example.test")
    {
        var account = await Register(client, email);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = await db.Roles.SingleAsync(r => r.Name == "Admin");
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = account.IdentityUserId!, RoleId = role.Id });
        await db.SaveChangesAsync();
        return await Login(client, email);
    }

    [Fact]
    public async Task GuestsAndCreatorsCannotListOrToggleUsers()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsJsonAsync("/api/admin/users/any/status", new { isActive = false })).StatusCode);
        var user = await Register(client, "creator@example.test");
        Auth(client, user.Token!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/admin/users/{user.IdentityUserId}/status", new { isActive = false })).StatusCode);
    }

    [Fact]
    public async Task AdminListExcludesAdminsAndCannotChangeProtectedFields()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var admin = await MakeAdmin(factory, client);
        var secondAdmin = await MakeAdmin(factory, client, "secondadmin@example.test");
        var user = await Register(client, "creator@example.test");
        Auth(client, admin.Token!);
        var list = (await client.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users"))!;
        Assert.Single(list);
        Assert.Equal(user.IdentityUserId, list[0].IdentityUserId);
        Assert.Equal("0912345678", list[0].ContactPhone);
        foreach (var id in new[] { admin.IdentityUserId, secondAdmin.IdentityUserId, "missing" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/admin/users/{id}/status", new { isActive = false })).StatusCode);
        var route = $"/api/admin/users/{user.IdentityUserId}/status";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync(route, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync(route, new {
            isActive = false, displayName = "hacked", email = "hacked@example.test", contactPhone = "000", role = "Admin", workStatus = 2
        })).StatusCode);
        var saved = Assert.Single((await client.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users"))!);
        Assert.False(saved.IsActive);
        Assert.Equal("creator", saved.DisplayName);
        Assert.Equal("creator@example.test", saved.Email);
        Assert.Equal("0912345678", saved.ContactPhone);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, (await db.CreatorProfiles.SingleAsync(p => p.IdentityUserId == user.IdentityUserId)).WorkStatus);
        Assert.Single(await db.UserRoles.Where(r => r.UserId == user.IdentityUserId).ToListAsync());
    }

    [Fact]
    public async Task DisableRevokesAllSessionsAndEnableRequiresFreshLoginEvenAfterRestart()
    {
        string first, second, adminToken, userId;
        await using (var factory = new TestServer(database))
        {
            using var client = factory.Client();
            adminToken = (await MakeAdmin(factory, client)).Token!;
            var user = await Register(client, "creator@example.test");
            userId = user.IdentityUserId!; first = user.Token!;
            second = (await Login(client, "creator@example.test")).Token!;
            Auth(client, adminToken);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync($"/api/admin/users/{userId}/status", new { isActive = false })).StatusCode);
            foreach (var token in new[] { first, second })
            {
                Auth(client, token);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Auth/me")).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/Auth/profile", new { displayName = "blocked" })).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsJsonAsync("/api/Auth/work-status", new { workStatus = 2 })).StatusCode);
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Auth/login", new { email = "creator@example.test", password = Password })).StatusCode);
        }
        await using var restarted = new TestServer(database);
        using var after = restarted.Client();
        Auth(after, adminToken);
        Assert.False(Assert.Single((await after.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users"))!).IsActive);
        Assert.Equal(HttpStatusCode.OK, (await after.PatchAsJsonAsync($"/api/admin/users/{userId}/status", new { isActive = true })).StatusCode);
        foreach (var token in new[] { first, second })
        {
            Auth(after, token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await after.GetAsync("/api/Auth/me")).StatusCode);
        }
        var fresh = await Login(after, "creator@example.test");
        Auth(after, fresh.Token!);
        Assert.Equal(HttpStatusCode.OK, (await after.GetAsync("/api/Auth/me")).StatusCode);
    }

    [Fact]
    public async Task RemovingAdminInDatabaseBlocksOldAdminToken()
    {
        await using var factory = new TestServer(database);
        using var client = factory.Client();
        var admin = await MakeAdmin(factory, client);
        var user = await Register(client, "creator@example.test");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var role = await db.Roles.SingleAsync(r => r.Name == "Admin");
            await db.UserRoles.Where(r => r.UserId == admin.IdentityUserId && r.RoleId == role.Id).ExecuteDeleteAsync();
        }
        Auth(client, admin.Token!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/admin/users/{user.IdentityUserId}/status", new { isActive = false })).StatusCode);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
