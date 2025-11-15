using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq;

namespace CoreOne.API.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenService _tokenService;
        private readonly DBContext _dbHelper;

        public AuthorizationMiddleware(RequestDelegate next, TokenService tokenService, DBContext dbHelper)
        {
            _next = next;
            _tokenService = tokenService;
            _dbHelper = dbHelper;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Skip public APIs
            if (path.Contains("/auth/login") || path.Contains("/auth/logout") || path.Contains("/auth/forgotpassword") || path.Contains("/auth/resetpassword")
               || path.Contains("/auth/create-cachekey") || path.Contains("/auth/exchange-cachekey") 
               ||  path.Contains("/auth/loghttperror") || path.Contains("/account/companyselection") || path.Contains("auth/create-company-selection-key")
                 || path.Contains("/auth/validate-company-selection") || path.Contains("/auth/logexception") || path.Contains("/index"))
            {
                await _next(context);
                return;
            }

            // 1️⃣ Validate JWT
            string authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();
            var principal = _tokenService.ValidateToken(token);

            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid");
                return;
            }

            context.User = principal;

            // 2️⃣ Extract user info
            int userID = int.Parse(principal.FindFirst("UserID")?.Value ?? "0");
            int roleID = int.Parse(principal.FindFirst("RoleID")?.Value ?? "0");

            // 3️⃣ Optional: Check module/action permission if provided as headers
            if (context.Request.Headers.TryGetValue("ModuleID", out var moduleIDHeader) &&
                context.Request.Headers.TryGetValue("ActionID", out var actionIDHeader))
            {
                int moduleID = int.Parse(moduleIDHeader);
                int actionID = int.Parse(actionIDHeader);

                int permissionCount = _dbHelper.ExecuteSP_ReturnInt("sp_Permission_CheckUserPermission",
                    new Dictionary<string, object>
                    {
                        {"@RoleID", roleID},
                        {"@ModuleID", moduleID},
                        {"@ActionID", actionID}
                    });

                if (permissionCount == 0)
                {
                    context.Response.StatusCode = 403; // Forbidden
                    await context.Response.WriteAsync("Access denied: no permission");
                    return;
                }
            }

            await _next(context);
        }
    }
}
