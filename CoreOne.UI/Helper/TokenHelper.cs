using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http;

namespace CoreOne.UI.Helper
{
    public static class TokenHelper
    {
       
            public static int? GetUserIdFromToken(HttpContext context)
            {
                var token = context.Request.Cookies["jwtToken"];
                if (string.IsNullOrEmpty(token)) return null;

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                    return userId;

                return null;
            }
        public static void RemoveToken(HttpContext context)
        {
            if (context.Request.Cookies.ContainsKey("jwtToken"))
            {
                context.Response.Cookies.Delete("jwtToken");
            }
        }

        public static User? UserFromToken(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt;

            try
            {
                jwt = handler.ReadJwtToken(token);
            }
            catch
            {
                return null;
            }

            // Extract claims
            var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            var userNameClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Email")?.Value;
            var mailTypeIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "EmailType")?.Value;
            var roleIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "RoleID")?.Value;
            var roleNameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "RoleName")?.Value;
            var phoneClaim = jwt.Claims.FirstOrDefault(c => c.Type == "PhoneNumber")?.Value;

            // Parse and assign to user model
            if (!int.TryParse(userIdClaim, out int userId)) return null;
            if (!int.TryParse(roleIdClaim, out int roleId)) return null;

            int? mailTypeId = null;
            if (int.TryParse(mailTypeIdClaim, out int parsedMailTypeId))
            {
                mailTypeId = parsedMailTypeId;
            }

            var user = new User
            {
                UserID = userId,
                UserName = userNameClaim ?? string.Empty,
                Email = emailClaim ?? string.Empty,
                MailTypeID = mailTypeId,
                RoleID = roleId,
                PhoneNumber = phoneClaim ?? string.Empty,
                RoleName = roleNameClaim ?? string.Empty,
                // Defaulting unset fields as they are not in token
                PasswordHash = string.Empty,
               
                ActiveFlag = true,  // Or false depending on your needs
                CreatedBy = userId       // Unknown from token
            };

            return user;
        }
        public static bool IsTokenValid(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return false;

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwt = handler.ReadJwtToken(token);

                // Get expiration claim (exp)
                var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

                if (expClaim == null) return false;

                // exp is in UNIX time (seconds since 1970-01-01)
                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;

                return expDate > DateTime.UtcNow; // true if not expired
            }
            catch
            {
                return false; // if parsing fails, consider it invalid
            }
        }



    }
}
