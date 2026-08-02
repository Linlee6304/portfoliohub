using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Services;
using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
var jwtKey = builder.Configuration["Jwt:Key"]
?? throw new InvalidOperationException("Jwt:Key 尚未設定");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthReopnsitory, AuthReopnsitory>();
builder.Services.AddScoped<ICreatorScheduleService, CreatorScheduleService>();
builder.Services.AddScoped<ICreatorScheduleRepository, CreatorScheduleRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();



var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

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

    var adminEmail = builder.Configuration["Admin:Email"]
        ?? throw new InvalidOperationException(
            "Admin:Email 尚未設定");

    var adminPassword = builder.Configuration["Admin:Password"]
        ?? throw new InvalidOperationException(
            "Admin:Password 尚未設定");

    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail
        };

        var createResult = await userManager.CreateAsync(
            admin,
            adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                createResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"建立管理員失敗：{errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(admin, "Admin"))
    {
        var roleResult = await userManager.AddToRoleAsync(
            admin,
            "Admin");

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                roleResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"加入管理員角色失敗：{errors}");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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