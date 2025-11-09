using CoreOne.DOMAIN.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;

namespace CoreOne.API.Infrastructure.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _privateKey;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _issuer = _configuration["JWT:Issuer"];
            _audience = _configuration["JWT:Audience"];
            _privateKey = _configuration["JWT:PrivateKey"];
        }

        private SymmetricSecurityKey GetKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_privateKey));

        /// <summary>
        /// Generates a single JWT for the user that includes all access info
        /// </summary>
        public string GenerateUserToken(User user, List<dynamic> accessList)
        {
            var companyAccess = accessList.Select(a => new
            {
                CompanyID = (int)a.CompanyID,
                CompanyName = a.CompanyName?.ToString(),
                ApplicationID = (int)a.ApplicationID,
                ApplicationName = a.ApplicationName?.ToString(),
                RoleID = (int)a.RoleID,
                RoleName = a.RoleName?.ToString()
            }).ToList();

            var claims = new List<Claim>
            {
                new Claim("UserID", user.UserID.ToString()),
                new Claim("UserName", user.UserName ?? ""),
                new Claim("Email", user.Email ?? ""),
                new Claim("IsInternal", user.IsInternal ? "true" : "false"),
                // full list of access as JSON
                new Claim("AccessMatrix", JsonConvert.SerializeObject(companyAccess)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // long enough for full session
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validates a JWT and returns ClaimsPrincipal
        /// </summary>
        public ClaimsPrincipal ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = GetKey();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            try
            {
                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
