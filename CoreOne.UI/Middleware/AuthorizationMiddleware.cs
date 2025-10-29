using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;

namespace CoreOne.UI.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        public AuthorizationMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Cookies["jwtToken"];
            var path = context.Request.Path.Value?.ToLower();

            // Skip login page
            if (string.IsNullOrEmpty(token) || path.Contains("/account/login") ||  path.Contains("/api/FileUpload/viewPost"))
            {
                await _next(context);
                return;
            }

            if (string.IsNullOrEmpty(token) || path.Contains("/Account/ForgotPassword") || path.Contains(" / api/FileUpload/viewPost"))
            {
                await _next(context);
                return;
            }
            // Skip static files & documents dynamically (any extension)
            if (Path.HasExtension(path))
            {
                await _next(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);

                // Extract user id from claims
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    await ForceLogout(context);
                    return;
                }

                // Check expiry (UTC)
                if (jwt.ValidTo <= DateTime.UtcNow)
                {
                    await CallLogoutApi(userId);
                    await ForceLogout(context);
                    return;
                }
            }
            catch
            {
                await ForceLogout(context);
                return;
            }

            // Token is valid → let [Authorize] handle role/claim access
            await _next(context);
        }

        private async Task CallLogoutApi(int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiSettings.BaseUrlAuthentication}/Logout";

                var json = JsonConvert.SerializeObject(new { UserId = userId });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await client.PostAsync(url, content);
            }
            catch
            {
                // Ignore logout API errors
            }
        }

        private async Task ForceLogout(HttpContext context)
        {
            // Delete token cookie
            context.Response.Cookies.Delete("jwtToken");

            // Clear storage and redirect to login
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(@"
                <script>
                    localStorage.clear();
                    sessionStorage.clear();
                    window.location.href = '/Account/Login';
                </script>
            ");
        }
    }
}
