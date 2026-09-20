using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
using PortfolioHub.Server.Services;
using Xunit;

namespace PortfolioHub.Tests;

public class WorkColumnMappingTests
{
    [Fact]
    public void SqlServerListQueryUsesStartDateColumn()
    {
        // 只產生 SQL，不連線到 SQL Server。
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        var sql = db.Works.Where(w => w.WorksCreators.Any(c => c.CreatorId == 1))
            .OrderByDescending(w => w.UpdatedAt).ToQueryString();
        Assert.Contains("[w].[StartDate]", sql);
        Assert.DoesNotContain("[w].[StarDate]", sql);
    }

    [Fact]
    public async Task ExistingStartDateSchemaSupportsEmptyListCreateAndUpdate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options);
        // 手動建立既有欄位名稱，不能用 EnsureCreated，否則錯誤模型會產生同樣錯誤的表。
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE Works (
                WorkId INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL, Description TEXT NULL,
                StartDate TEXT NULL, EndDate TEXT NULL,
                Status INTEGER NOT NULL, CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL, WorkType INTEGER NOT NULL
            );
            CREATE TABLE WorksCreators (
                WorkId INTEGER NOT NULL, CreatorId INTEGER NOT NULL,
                Role TEXT NOT NULL, SortOrder INTEGER NOT NULL,
                PRIMARY KEY (WorkId, CreatorId)
            );
            CREATE TABLE CreatorProfiles (
                CreatorId INTEGER PRIMARY KEY,
                IdentityUserId TEXT NOT NULL, IsActive INTEGER NOT NULL
            );
            INSERT INTO CreatorProfiles VALUES (1, 'test-user', 1);
            """);
        var repository = new WorkRepository(db);
        Assert.Empty(await repository.ListAsync(1, CancellationToken.None));
        var firstDate = new DateTime(2026, 9, 1);
        var work = await repository.CreateAsync(1, new Works {
            Title = "欄位回歸測試", Description = "", StarDate = firstDate,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(firstDate, (await repository.FindAsync(1, work.WorkId, CancellationToken.None))!.StarDate);
        var changedDate = new DateTime(2026, 9, 2);
        await repository.UpdateAsync(1, work.WorkId, "更新日期", "", changedDate, null, DateTime.UtcNow, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(changedDate, Assert.Single(await repository.ListAsync(1, CancellationToken.None)).StarDate);
        // 模擬既有資料中的 NULL 描述，API 應轉為空字串供前端表單使用。
        await db.Database.ExecuteSqlRawAsync("UPDATE Works SET Description = NULL");
        db.ChangeTracker.Clear();
        var service = new WorkService(repository);
        var result = await service.ListAsync("test-user", CancellationToken.None);
        Assert.Equal("", Assert.Single(result.Items).Description);
    }
}
