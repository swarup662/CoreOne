using System.IdentityModel.Tokens.Jwt;
using CoreOne.COMMON.Models;
using Microsoft.AspNetCore.Http;

namespace CoreOne.UI.Helper
{
    public static class TokenHelper
    {
        // Returns user id or null
        public static int? GetUserIdFromCookie(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);
                var claim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
                if (int.TryParse(claim, out var id)) return id;
            }
            catch { }
            return null;
        }

        // Return User object if you have the claims in token
        public static User? UserFromToken(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);
                var u = new User
                {
                    UserID = int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value, out var id) ? id : 0,
                    UserName = jwt.Claims.FirstOrDefault(c => c.Type == "UserName")?.Value,
                    RoleID = int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "RoleID")?.Value, out var r) ? r : 0
                };
                return u;
            }
            catch { }
            return null;
        }
    }
}
