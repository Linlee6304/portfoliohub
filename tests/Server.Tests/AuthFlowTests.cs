using System.Net;
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
        var admin = await Login(client, "admin@example.test", "TestAdmin!23456");
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
    public async Task ProfileUsesAuthenticatedUserAndAllowsNewEmailWithoutEndingSession()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var first = await RegisterAndLogin(client);
        var second = await RegisterAndLogin(client, "other@example.test");
        Authorize(client, first.Token!);
        var update = await client.PutAsJsonAsync("/api/Auth/profile", new
        {
            identityUserId = second.IdentityUserId, email = "changed@example.test",
            displayName = "新的名字", workStatus = 2, bio = "更新介紹"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var me = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal(first.IdentityUserId, me!.IdentityUserId);
        Assert.Equal("新的名字", me.DisplayName);
        Assert.Equal("changed@example.test", me.Email);
        Authorize(client, second.Token!);
        me = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal("other@example.test", me!.Email);
        await Login(client, "changed@example.test");
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
    public async Task DuplicateEmailAndUnsafeAvatarAreRejected()
    {
        await using var factory = Factory();
        using var client = factory.Client();
        var first = await RegisterAndLogin(client);
        await RegisterAndLogin(client, "other@example.test");
        Authorize(client, first.Token!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/Auth/profile", new
        { email = "other@example.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/Auth/profile", new
        { email = "creator@example.test", avatarUrl = "javascript:alert(1)" })).StatusCode);
        var account = await client.GetFromJsonAsync<ResponseGetAccountDto>("/api/Auth/me");
        Assert.Equal("creator@example.test", account!.Email);
    }
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
