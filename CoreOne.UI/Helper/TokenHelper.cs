using System.IdentityModel.Tokens.Jwt;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CoreOne.UI.Helper
{
    public static class TokenHelper
    {
        // ---------------------------
        // 1. Get user id
        // ---------------------------
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

        // ---------------------------
        // 2. Remove token + selection cookies
        // ---------------------------
        public static void RemoveToken(HttpContext context)
        {
            // JWT authentication cookie
            context.Response.Cookies.Delete("jwtToken");

            // Secure encrypted context cookie
            context.Response.Cookies.Delete("UserCtx");

            // UI selection cookies (harmless but must be removed)
            context.Response.Cookies.Delete("CurrentCompanyID");
            context.Response.Cookies.Delete("CurrentApplicationID");
            context.Response.Cookies.Delete("CurrentRoleID");

            // ASP.NET Core antiforgery cookies – optional but recommended on logout
            context.Response.Cookies.Delete(".AspNetCore.Antiforgery");
            context.Response.Cookies.Delete(".AspNetCore.Session");
            context.Response.Cookies.Delete("TempData");

            // In case TempData uses cookie-based provider
            context.Response.Cookies.Delete(".AspNetCore.Mvc.CookieTempDataProvider");
        }


        // ---------------------------
        // 3. Validate token exp
        // ---------------------------
        public static bool IsTokenValid(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

                var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                if (expClaim == null) return false;

                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;
                return expDate > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------
        // 4. FULL secure current user extraction
        // ---------------------------
        public static CurrentUserDetail? UserFromToken(HttpContext context, SignedCookieHelper cookieHelper)
        {
            var token = context.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token)) return null;

            JwtSecurityToken jwt;
            try { jwt = new JwtSecurityTokenHandler().ReadJwtToken(token); }
            catch { return null; }

            // Basic Claims
            int userId = int.Parse(jwt.Claims.First(x => x.Type == "UserID").Value);
            string userName = jwt.Claims.FirstOrDefault(x => x.Type == "UserName")?.Value ?? "";
            string email = jwt.Claims.FirstOrDefault(x => x.Type == "Email")?.Value ?? "";
            string phone = jwt.Claims.FirstOrDefault(x => x.Type == "PhoneNumber")?.Value ?? "";

            int? mailTypeID = int.TryParse(
                jwt.Claims.FirstOrDefault(x => x.Type == "EmailType")?.Value,
                out var m
            ) ? m : null;

            bool isInternal = jwt.Claims.First(x => x.Type == "IsInternal").Value == "true";

            // Deserialize AccessMatrix
            string accessJson = CompressionHelper.DecompressFromBase64(
                jwt.Claims.First(x => x.Type == "AccessMatrix").Value
            );
            var accessList = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(accessJson);

            // Deserialize Roles
            string rolesJson = CompressionHelper.DecompressFromBase64(
                jwt.Claims.First(x => x.Type == "RoleList").Value
            );
            var roles = JsonConvert.DeserializeObject<List<Roles>>(rolesJson);

            // Get ACTIVE secure context
            var activeCtx = GetActiveContext(context, cookieHelper, accessList);

            // Build model
            return new CurrentUserDetail
            {
                UserID = userId,
                UserName = userName,
                Email = email,
                PhoneNumber = phone,
                MailTypeID = mailTypeID,
                IsInternal = isInternal,
                ActiveFlag = true,
                CreatedBy = userId,

                CurrentCompanyID = activeCtx.CompanyID,
                CurrentApplicationID = activeCtx.ApplicationID,
                CurrentRoleID = activeCtx.RoleID,

                UserAccessList = accessList,
                Roles = roles
            };
        }


        private static int ReadCookieInt(HttpContext ctx, string key, int fallback)
        {
            if (ctx.Request.Cookies.TryGetValue(key, out string? val))
                if (int.TryParse(val, out int parsed))
                    return parsed;

            return fallback;
        }

        // 1. Read secure cookie → SINGLE active context
        public static UserContextModel? GetSecureContext(
            HttpContext ctx,
            SignedCookieHelper cookieHelper)
        {
            if (!ctx.Request.Cookies.TryGetValue("UserCtx", out string? signed))
                return null;

            var raw = cookieHelper.ValidateAndGet(signed);
            if (raw == null) return null;

            var parts = raw.Split('|');
            if (parts.Length != 3) return null;

            if (int.TryParse(parts[0], out int comp) &&
                int.TryParse(parts[1], out int app) &&
                int.TryParse(parts[2], out int role))
            {
                return new UserContextModel
                {
                    CompanyID = comp,
                    ApplicationID = app,
                    RoleID = role
                };
            }

            return null;
        }

        // 2. Validate + Determine Active Context
        public static UserContextModel GetActiveContext(
            HttpContext ctx,
            SignedCookieHelper cookieHelper,
            List<UserAccessViewModel> accessList)
        {
            var selected = GetSecureContext(ctx, cookieHelper);

            if (selected != null)
            {
                bool valid = accessList.Any(a =>
                    a.CompanyID == selected.CompanyID &&
                    a.ApplicationID == selected.ApplicationID &&
                    a.RoleID == selected.RoleID
                );

                if (valid)
                    return selected;
            }

            // Fallback → first item from AccessMatrix
            var first = accessList.First();

            return new UserContextModel
            {
                CompanyID = first.CompanyID,
                ApplicationID = first.ApplicationID,
                RoleID = first.RoleID
            };
        }


    }
}
