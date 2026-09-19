using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.Services;
using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key 尚未設定");
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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var stamp = context.Principal?.FindFirstValue("security_stamp");
                var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                if (!await auth.IsSessionValid(userId, tokenId, stamp))
                {
                    context.Fail("登入已失效，請重新登入");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
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

    // 只建立角色定義；管理員帳戶與角色由管理者直接在資料庫維護。

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

public partial class Program { }
