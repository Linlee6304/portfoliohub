using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Services;
using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthReopnsitory, AuthReopnsitory>();
builder.Services.AddScoped<ICreatorScheduleService, CreatorScheduleService>();
builder.Services.AddScoped<ICreatorScheduleRepository, CreatorScheduleRepository>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();



var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Admin"));
    }

    if (!await roleManager.RoleExistsAsync("Creator"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Creator"));
    }
}

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