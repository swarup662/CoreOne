using System.IdentityModel.Tokens.Jwt;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CoreOne.API.Helper
{
    public static class TokenHelper
    {
        // -------- 1. Get user id --------
        public static int? GetUserIdFromToken(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var claim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
                return int.TryParse(claim, out var id) ? id : null;
            }
            catch { return null; }
        }

        // -------- 2. Remove token --------
        public static void RemoveToken(HttpContext context)
        {
            if (context.Request.Cookies.ContainsKey("jwtToken"))
                context.Response.Cookies.Delete("jwtToken");
        }

        // -------- 3. Extract CurrentUserDetail --------
        public static CurrentUserDetail? CurrentUserFromToken(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;

            JwtSecurityToken jwt;
            try
            {
                jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            }
            catch { return null; }

            // Basic claims
            int userId = int.Parse(jwt.Claims.First(x => x.Type == "UserID").Value);
            string userName = jwt.Claims.FirstOrDefault(x => x.Type == "UserName")?.Value ?? "";
            string email = jwt.Claims.FirstOrDefault(x => x.Type == "Email")?.Value ?? "";
            string phone = jwt.Claims.FirstOrDefault(x => x.Type == "PhoneNumber")?.Value ?? "";
            string mailTypeStr = jwt.Claims.FirstOrDefault(x => x.Type == "EmailType")?.Value;

            int? mailTypeID = null;
            if (int.TryParse(mailTypeStr, out int m)) mailTypeID = m;

            bool isInternal = jwt.Claims.First(x => x.Type == "IsInternal").Value == "true";

            // Defaults
            int defaultCompany = int.Parse(jwt.Claims.First(x => x.Type == "DefaultCompanyID").Value);
            int defaultApp = int.Parse(jwt.Claims.First(x => x.Type == "DefaultApplicationID").Value);
            int defaultRole = int.Parse(jwt.Claims.First(x => x.Type == "DefaultRoleID").Value);

            // AccessMatrix
            string matrixJson = CompressionHelper.DecompressFromBase64(
                jwt.Claims.First(x => x.Type == "AccessMatrix").Value
            );
            var accessList = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(matrixJson);

            // Roles
            string rolesJson = CompressionHelper.DecompressFromBase64(
                jwt.Claims.First(x => x.Type == "RoleList").Value
            );
            var roles = JsonConvert.DeserializeObject<List<Roles>>(rolesJson);

            return new CurrentUserDetail
            {
                UserID = userId,
                UserName = userName,
                Email = email,
                MailTypeID = mailTypeID,
                PhoneNumber = phone,
                IsInternal = isInternal,

                ActiveFlag = true,
                CreatedBy = userId,

                CurrentCompanyID = defaultCompany,
                CurrentApplicationID = defaultApp,
                CurrentRoleID = defaultRole,

                UserAccessList = accessList,
                Roles = roles
            };
        }
    }
}
