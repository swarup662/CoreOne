using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using CoreOne.UI.Controllers;
using CoreOne.UI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;


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
            var path = context.Request.Path.Value?.Split('?')[0].ToLower() ?? "";


            // Skip login page
            if (string.IsNullOrEmpty(token) || path.Contains("/account/login") || path.Contains("/api/FileUpload/viewPost") || path.Contains("/account/forgotpassword")
                || path.Contains("/account/resetpassword") || path.Contains("/auth/getswitchoptions") || path.Contains("/account/logout"))
            {
                await _next(context);
                return;
            }
            else
            {
                string[] alwaysAllowedViews =
                                {
                    "/dashboard",
                    "/profile",
                    "/home/index",
                    "/error",
                    "/home/error"
                };

                // ✅ If no token -> bypass middleware
                if (string.IsNullOrEmpty(token))
                {
                    await _next(context);
                    return;
                }

                // ✅ Fetch menu and capture MenuID early
                var menuService = context.RequestServices.GetRequiredService<IMenuService>();
                var menuList = await menuService.GetUserMenu(context);

                var matchedMenu = menuList.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Url) &&
                    path.StartsWith(m.Url.ToLower())
                );

                // ✅ Store MenuID and full menulist (for layout)
                if (matchedMenu != null)
                {
                    context.Items["MenuID"] = matchedMenu.MenuID;
                    context.Items["UserMenu"] = menuList;
                }

                var endpoint = context.GetEndpoint();

                // Get MVC action descriptor (if MVC)
                var action = endpoint?.Metadata
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault();

                bool isMvcAction = action != null;

                // ✅ Detect API endpoint based on ApiController attribute
                bool isApiEndpoint = endpoint?.Metadata
                    .Any(m => m.GetType().Name == "ApiControllerAttribute") == true;

                // ✅ Detect real view rendering (ViewResult or PartialViewResult)
                bool isViewAction =
                     typeof(ViewResult).IsAssignableFrom(action?.MethodInfo.ReturnType) ||
                     typeof(PartialViewResult).IsAssignableFrom(action?.MethodInfo.ReturnType);


                // ------------------------------------
                // 🚧 AUTHORIZATION LOGIC STARTS HERE
                // ------------------------------------

                // ✅ Allow API
                if (isApiEndpoint)
                {
                    await _next(context);
                    return;
                }

                // ✅ Allow MVC actions that are NOT returning a full View (JSON / PartialView / file result / redirect, etc.)
                if (isMvcAction && !isViewAction)
                {
                    await _next(context);
                    return;
                }

                // ✅ Allow always allowed full Views
                if (isViewAction && alwaysAllowedViews.Any(v => path.StartsWith(v)))
                {
                    await _next(context);
                    return;
                }

                // ✅ If it's a full View (ViewResult) and NOT in menu → Access Denied
                if (matchedMenu == null)
                {
                    context.Response.Redirect($"/Home/Error?statusCode=403&errorMessage=Access denied");
                    return;
                }
;


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
                var url = $"{_apiSettings.BaseUrlAuth}/Logout";

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
