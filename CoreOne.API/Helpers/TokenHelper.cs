using System.IdentityModel.Tokens.Jwt;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CoreOne.API.Helper
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
            var userNameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserName")?.Value;
            var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "Email")?.Value;
            var mailTypeIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "EmailType")?.Value;

            var rolesClaim = jwt.Claims.FirstOrDefault(c => c.Type == "RoleList")?.Value;
            var phoneClaim = jwt.Claims.FirstOrDefault(c => c.Type == "PhoneNumber")?.Value;
            var accessListClaim = jwt.Claims.FirstOrDefault(c => c.Type == "AccessMatrix")?.Value;
            // Parse and assign to user model
            if (!int.TryParse(userIdClaim, out int userId)) return null;


            int? mailTypeId = null;
            if (int.TryParse(mailTypeIdClaim, out int parsedMailTypeId))
            {
                mailTypeId = parsedMailTypeId;
            }
            var AccessMatrixJson = CompressionHelper.DecompressFromBase64(accessListClaim);
            List<UserAccessViewModel> AccessMatrix = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(AccessMatrixJson);
            var RolesJson = CompressionHelper.DecompressFromBase64(rolesClaim);
            List<Roles> Roles = JsonConvert.DeserializeObject<List<Roles>>(AccessMatrixJson);
            var user = new User
            {
                UserID = userId,
                UserName = userNameClaim ?? string.Empty,
                Email = emailClaim ?? string.Empty,
                MailTypeID = mailTypeId,

                PhoneNumber = phoneClaim ?? string.Empty,

                // Defaulting unset fields as they are not in token
                PasswordHash = string.Empty,

                ActiveFlag = true,  // Or false depending on your needs

                UserAccessList = AccessMatrix,
                Roles = Roles,
            };


            return user;
        }
    }
}
