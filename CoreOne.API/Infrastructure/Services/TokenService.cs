using CoreOne.DOMAIN.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using CoreOne.API.Helper;

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

        public string GenerateUserToken(User user, List<UserAccessViewModel> accessList)
        {
            // Security: DO NOT issue token with empty list
            if (accessList == null || accessList.Count == 0)
                throw new Exception("Access list cannot be empty");

            // Build roles
            var roles = accessList
                .DistinctBy(a => a.RoleID)
                .Select(a => new Roles
                {
                    RoleID = a.RoleID,
                    RoleName = a.RoleName
                })
                .ToList();

            // Defaults
            int defaultCompany = accessList[0].CompanyID;
            int defaultApp = accessList[0].ApplicationID;
            int defaultRole = accessList[0].RoleID;

            // Compress AccessMatrix + Roles
            string compressedAccess = CompressionHelper.CompressToBase64(JsonConvert.SerializeObject(accessList));
            string compressedRoles = CompressionHelper.CompressToBase64(JsonConvert.SerializeObject(roles));

            var claims = new List<Claim>
            {
                new Claim("UserID", user.UserID.ToString()),
                new Claim("UserName", user.UserName),
                new Claim("Email", user.Email ?? ""),
                new Claim("PhoneNumber", user.PhoneNumber ?? ""),
                new Claim("EmailType", user.MailTypeID?.ToString() ?? ""),
                new Claim("IsInternal", user.IsInternal ? "true" : "false"),

                // DEFAULTS
                new Claim("DefaultCompanyID", defaultCompany.ToString()),
                new Claim("DefaultApplicationID", defaultApp.ToString()),
                new Claim("DefaultRoleID", defaultRole.ToString()),

                // FULL ACCESS MATRIX + ROLES
                new Claim("AccessMatrix", compressedAccess),
                new Claim("RoleList", compressedRoles),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = GetKey();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,

                ValidateAudience = true,
                ValidAudience = _audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(10),

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
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
