namespace PortfolioHub.Server.DTOs
{

    public class RequestLoginRegisterDto//註冊登入用DTO
    {
        public string Email { get; set; }
        public string Password { get; set; }//密碼
        public string? ConfirmPassword { get; set; }//二次密碼驗證
        public string? DisplayName { get; set; }
        public string? ContactPhone { get; set; }
    }
    public class RequestAuthDto
    {
        public string IdentityUserId { get; set; } = null!;

        // 修改後的新登入信箱，同時也是公開聯絡信箱
        public string Email { get; set; } = null!;

        public string? DisplayName { get; set; }
        public string? ContactPhone { get; set; }
        public string? AvatarUrl { get; set; }
        public int? WorkStatus { get; set; }
        public string? Bio { get; set; }
    }
    public class ResponseAuthDto//帳號相關回傳用DTO
    {
        public string Message { get; set; }
    }
    public class ResponseAuthInfoDto : ResponseAuthDto
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Role { get; set; }
        public string? IdentityUserId { get; set; }
    }

    public class ResponseGetAccountDto : ResponseAuthDto
    {
        public string? IdentityUserId { get; set; }
        public string? Email { get; set; }

        public string? DisplayName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? AvatarUrl { get; set; }
        public int? WorkStatus { get; set; }
        public string? Bio { get; set; }

        public bool? IsEmailExists { get; set; }
        public bool? IsIdentityUserIdExists { get; set; }
    }
}