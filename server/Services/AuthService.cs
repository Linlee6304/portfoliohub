using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Data;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
namespace PortfolioHub.Server.Services;

public class AuthService(IAuthReopnsitory authRepository, UserManager<ApplicationUser> userManager,
    AppDbContext context, IJwtService jwtService) : IAuthService
{
    public async Task<ResponseAuthInfoDto> Login(RequestLoginRegisterDto request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) return new() { Message = "帳號不存在" };
        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return new() { Message = "密碼錯誤" };
        var account = await GetCurrentUser(user.Id);
        if (!account.Success) return new() { Message = account.Message };
        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);
        return new()
        {
            Success = true, Message = "登入成功", Role = account.Role,
            DisplayName = account.DisplayName, AvatarUrl = account.AvatarUrl,
            IdentityUserId = user.Id, Email = user.Email, WorkStatus = account.WorkStatus,
            Token = token, TokenExpiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo
        };
    }

    public async Task<ResponseAuthDto> Register(RequestLoginRegisterDto request)
    {
        if (request.Password != request.ConfirmPassword) return new() { Message = "兩次輸入的密碼不一致" };
        if (request.Password.Length < 6) return new() { Message = "密碼長度至少 6 個字元" };
        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null) return new() { Message = "此信箱已註冊" };
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = new ApplicationUser { UserName = email, Email = email };
        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded) return new() { Message = DescribeErrors(create) };
        var role = await userManager.AddToRoleAsync(user, "Creator");
        if (!role.Succeeded) return new() { Message = "無法建立創作者帳號，請稍後再試" };
        await authRepository.CreateCreatorProfile(new CreatorProfiles
        {
            IdentityUserId = user.Id,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            ContactEmail = email, ContactPhone = request.ContactPhone?.Trim(),
            IsActive = true, WorkStatus = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await transaction.CommitAsync();
        return new() { Success = true, Message = "註冊成功，請登入" };
    }

    public async Task<ResponseGetAccountDto> GetCurrentUser(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return new() { Message = "登入帳號不存在" };
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
            return new()
            {
                Success = true, Message = "查詢成功", IdentityUserId = user.Id,
                Email = user.Email, ContactEmail = user.Email, DisplayName = user.UserName, Role = "Admin"
            };
        if (!roles.Contains("Creator")) return new() { Message = "此帳號未分配角色，請聯繫管理員" };
        var profile = await context.CreatorProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        if (profile is null || !profile.IsActive) return new() { Message = "創作者資料不存在或已停用，請聯繫管理員" };
        return new()
        {
            Success = true, Message = "查詢成功", IdentityUserId = user.Id,
            Email = user.Email, ContactEmail = profile.ContactEmail,
            DisplayName = profile.DisplayName, ContactPhone = profile.ContactPhone,
            AvatarUrl = profile.AvatarUrl, WorkStatus = profile.WorkStatus,
            Bio = profile.Bio, Role = "Creator", IsEmailExists = true, IsIdentityUserIdExists = true
        };
    }

    public async Task<ResponseGetAccountDto> GetAccountByEmail(string email)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null ? new() { Message = "查無此帳號" } : await GetCurrentUser(user.Id);
    }

    public async Task<ResponseGetAccountDto> UpdateAccount(RequestAuthDto request)
    {
        var user = await userManager.FindByIdAsync(request.IdentityUserId);
        if (user is null) return new() { Message = "登入帳號不存在" };
        var profile = await context.CreatorProfiles.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
        if (profile is null) return new() { Message = "此帳號沒有創作者基本資料" };
        var email = request.Email.Trim();
        var owner = await userManager.FindByEmailAsync(email);
        if (owner is not null && owner.Id != user.Id) return new() { Message = "此信箱已由其他帳號使用" };
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
            (!Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out var avatar) ||
             (avatar.Scheme != Uri.UriSchemeHttps && avatar.Scheme != Uri.UriSchemeHttp)))
            return new() { Message = "頭像請填入有效的圖片網址" };
        await using var transaction = await context.Database.BeginTransactionAsync();
        // UpdateAsync validates/normalizes both fields without rotating the password stamp.
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            user.EmailConfirmed = false;
        user.Email = email;
        user.UserName = email;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return new() { Message = DescribeErrors(updateResult) };
        profile.ContactEmail = email;
        profile.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();
        profile.ContactPhone = request.ContactPhone?.Trim();
        profile.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        profile.Bio = request.Bio?.Trim() ?? string.Empty;
        if (request.WorkStatus.HasValue) profile.WorkStatus = request.WorkStatus.Value;
        profile.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        var result = await GetCurrentUser(user.Id);
        result.Message = "基本資料已儲存";
        return result;
    }

    public async Task<ResponseChangePasswordDto> ChangePassword(RequestChangePasswordDto request)
    {
        if (request.NewPassword != request.ConfirmNewPassword) return new() { Message = "新密碼與確認密碼不一致" };
        if (request.CurrentPassword == request.NewPassword) return new() { Message = "新密碼不可與目前密碼相同" };
        var user = await userManager.FindByIdAsync(request.IdentityUserId);
        if (user is null) return new() { Message = "登入帳號不存在" };
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        // Identity rotates SecurityStamp, invalidating all JWTs issued before this change.
        return result.Succeeded
            ? new() { Success = true, Message = "密碼已更新，請重新登入" }
            : new() { Message = DescribeErrors(result), Errors = result.Errors.Select(e => e.Code) };
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join("；", result.Errors.Select(e => e.Code switch
        {
            "PasswordMismatch" => "目前密碼不正確",
            "PasswordTooShort" => "密碼長度至少 6 個字元",
            "PasswordRequiresDigit" => "密碼需包含數字",
            "PasswordRequiresLower" => "密碼需包含小寫英文字母",
            "PasswordRequiresUpper" => "密碼需包含大寫英文字母",
            "PasswordRequiresNonAlphanumeric" => "密碼需包含符號",
            "DuplicateEmail" or "DuplicateUserName" => "此信箱已註冊",
            "InvalidEmail" or "InvalidUserName" => "請填入有效的電子信箱",
            _ => "資料無法儲存，請檢查輸入內容或稍後再試"
        }));
}
