using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using PortfolioHub.Server.DTOs;
using PortfolioHub.Server.Models.Entities;
using PortfolioHub.Server.Repositories;
namespace PortfolioHub.Server.Services;

public class AuthService(IAuthReopnsitory authRepository, IJwtService jwtService) : IAuthService
{
    public async Task<ResponseAuthInfoDto> Login(RequestLoginRegisterDto request)
    {
        var user = await authRepository.FindByEmail(request.Email.Trim());
        if (user is null) return new() { Message = "帳號不存在" };
        if (!await authRepository.CheckPassword(user, request.Password))
            return new() { Message = "密碼錯誤" };
        var account = await GetCurrentUser(user.Id);
        if (!account.Success) return new() { Message = account.Message };
        var roles = await authRepository.GetRoles(user);
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
        if (await authRepository.FindByEmail(email) is not null) return new() { Message = "此信箱已註冊" };
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await authRepository.CreateAccount(user, request.Password, new CreatorProfiles
        {
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            ContactEmail = email, ContactPhone = request.ContactPhone?.Trim(),
            IsActive = true, WorkStatus = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        if (!result.Succeeded) return new() { Message = DescribeErrors(result) };
        return new() { Success = true, Message = "註冊成功，請登入" };
    }

    public async Task<ResponseGetAccountDto> GetCurrentUser(string userId)
    {
        var user = await authRepository.FindById(userId);
        if (user is null) return new() { Message = "登入帳號不存在" };
        var roles = await authRepository.GetRoles(user);
        var isAdmin = roles.Contains("Admin");
        if (!isAdmin && !roles.Contains("Creator")) return new() { Message = "此帳號未分配角色，請聯繫管理員" };
        var profile = await authRepository.GetProfile(userId);
        if (profile is null && isAdmin)
            return new() { Success = true, Message = "查詢成功", IdentityUserId = user.Id,
                Email = user.Email, ContactEmail = user.Email, DisplayName = user.UserName, Role = "Admin" };
        if (profile is null || !profile.IsActive) return new() { Message = "創作者資料不存在或已停用，請聯繫管理員" };
        return new()
        {
            Success = true, Message = "查詢成功", IdentityUserId = user.Id,
            Email = user.Email, ContactEmail = profile.ContactEmail,
            DisplayName = profile.DisplayName, ContactPhone = profile.ContactPhone,
            AvatarUrl = profile.AvatarUrl, WorkStatus = profile.WorkStatus,
            Bio = profile.Bio, Role = isAdmin ? "Admin" : "Creator"
        };
    }

    public async Task<ResponseGetAccountDto> UpdateAccount(string userId, RequestUpdateProfileDto request)
    {
        var account = await GetCurrentUser(userId);
        if (!account.Success) return account;
        if (string.IsNullOrWhiteSpace(request.DisplayName)) return new() { Message = "請填入暱稱" };
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
            (!Uri.TryCreate(request.AvatarUrl.Trim(), UriKind.Absolute, out var avatar) ||
             (avatar.Scheme != Uri.UriSchemeHttps && avatar.Scheme != Uri.UriSchemeHttp)))
            return new() { Message = "頭像請填入有效的圖片網址" };
        if (!await authRepository.UpdateProfile(userId, request.DisplayName.Trim(), request.ContactPhone?.Trim(),
            string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(), request.Bio?.Trim() ?? string.Empty))
            return new() { Message = "此帳號沒有可更新的基本資料" };
        var result = await GetCurrentUser(userId);
        result.Message = "儲存成功";
        return result;
    }

    public async Task<ResponseWorkStatusDto> UpdateWorkStatus(string userId, RequestWorkStatusDto request)
    {
        if (request.WorkStatus is not (>= 0 and <= 2)) return new() { Message = "接案狀態無效" };
        var account = await GetCurrentUser(userId);
        if (!account.Success) return new() { Message = account.Message };
        if (!await authRepository.UpdateWorkStatus(userId, request.WorkStatus.Value))
            return new() { Message = "此帳號沒有可更新的基本資料" };
        return new() { Success = true, Message = "接案狀態已儲存", WorkStatus = request.WorkStatus.Value };
    }

    public Task Logout(string userId, string tokenId, long expiresAt) => authRepository.Revoke(userId, tokenId, expiresAt);

    public async Task<bool> IsSessionValid(string? userId, string? tokenId, string? stamp)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tokenId) || string.IsNullOrEmpty(stamp)) return false;
        var user = await authRepository.FindById(userId);
        return user is not null && user.SecurityStamp == stamp && !await authRepository.IsRevoked(userId, tokenId);
    }

    public async Task<ResponseChangePasswordDto> ChangePassword(string userId, RequestChangePasswordDto request)
    {
        if (request.NewPassword != request.ConfirmNewPassword) return new() { Message = "新密碼與確認密碼不一致" };
        if (request.CurrentPassword == request.NewPassword) return new() { Message = "新密碼不可與目前密碼相同" };
        var user = await authRepository.FindById(userId);
        if (user is null) return new() { Message = "登入帳號不存在" };
        var result = await authRepository.ChangePassword(user, request.CurrentPassword, request.NewPassword);
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
