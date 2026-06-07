namespace PortfolioHub.Server.DTOs
{

    public class RequestLoginRegisterDto//註冊登入用DTO
    {
        public string Email { get; set; }
        public string Password { get; set; }//密碼
        public string? ConfirmPassword { get; set; }//二次密碼驗證
        public string? DisplayName { get; set; }
        public string? ContactEmail { get; set; }//聯絡Email
        public string? ContactPhone { get; set; }
    }
    public class RequestAuthDto//修改帳號用DTO
    {

        public string Email { get; set; }
        public string Password { get; set; }

        public string? DisplayName { get; set; }//創作者名稱
        public string? ContactEmail { get; set; }//聯絡Email
        public string? ContactPhone { get; set; }

    }
    public class ResponseAuthDto//帳號相關回傳用DTO
    {
        public string Message { get; set; }
    }
}