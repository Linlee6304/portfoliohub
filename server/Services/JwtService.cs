using Microsoft.IdentityModel.Tokens;
using PortfolioHub.Server.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PortfolioHub.Server.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(
            ApplicationUser user,
            IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key 尚未設定");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var claims = new List<Claim>
            {
                // 使用者識別
                new(ClaimTypes.NameIdentifier, user.Id),

                // 登入信箱
                new(ClaimTypes.Email, user.Email ?? string.Empty),

                // 帳號(目前就是 Email)
                new(ClaimTypes.Name, user.UserName ?? string.Empty),

                // Token 唯一識別碼
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // 權限
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}