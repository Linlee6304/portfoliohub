using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/api/test-db", async (AppDbContext db) =>
{
    var count = await db.Works.CountAsync();

    return Results.Ok(new
    {
        message = "DB connected",
        worksCount = count
    });
});

app.Run();