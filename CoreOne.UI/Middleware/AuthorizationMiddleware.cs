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
            
            var existendpoint = context.GetEndpoint();

            // ❌ If endpoint is null → route does not exist → return 404
            if (existendpoint == null)
            {
                context.Response.Redirect("/Home/Error?statusCode=404&errorMessage=Page not found");
                return;
            }

            // Skip login-related and public pages
            if (path.Contains("/account/login")
                || path.Contains("/api/fileupload/viewpost")
                || path.Contains("/account/forgotpassword")
                || path.Contains("/account/resetpassword")
                || path.Contains("/auth/getswitchoptions")
                || path.Contains("/account/logout")
                      || path.Contains("/account/companyselection")
                      || path.Contains("/account/getapplicationsbycompany")
                      || path.Contains("/account/getcompanies")
                      || path.Contains("/account/urlexistsasync")
                      || path.Contains("/account/launchApp")
                      || path.Contains("/account/validatecompanyselection")
                          || path.Contains("/account/launchapp")
                      || path.Contains("/account/companylogout")
                         || path.Contains("/account/cleartempData")
                          || path.Contains("/auth/consume")
                             || path.Contains("/auth/setusercontext")
                                || path.Contains("/auth/getcurrentcontext")
                                      || path.Contains("/notification/clearall")
                                            || path.Contains("/notification/Markasread")
                )
            { 
                await _next(context);
                return;
            }
            else
            {
                bool IsTokenValid = TokenHelper.IsTokenValid(context);
                if (!IsTokenValid)
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }

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

              
                var endpoint = context.GetEndpoint();

                // Get MVC action descriptor (if MVC)
                var action = endpoint?.Metadata
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault();

                bool isMvcAction = action != null;

                // ✅ Detect API endpoint based on ApiController attribute
                bool isApiEndpoint = endpoint?.Metadata
                    .Any(m => m.GetType().Name == "ApiControllerAttribute") == true;
          ;

                // ✅ Detect real view rendering (ViewResult or PartialViewResult)

                bool isViewAction = IsRealViewAction(action);


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
                // ✅ Fetch menu and capture MenuID early

                bool TokenValid = TokenHelper.IsTokenValid(context);

                MenuItem matchedMenu = new MenuItem();
                List<MenuItem> menuList = new List<MenuItem>();
                if (TokenValid)
                {

                    var menuService = context.RequestServices.GetRequiredService<IMenuService>();
                    menuList = await menuService.GetUserMenu(context);
                    if (menuList is null || menuList.Count == 0)
                    {
                        context.Response.Redirect("/Home/Error?statusCode=403&errorMessage=Access Denied");
                        return;
                    }
                    matchedMenu = menuList.FirstOrDefault(m =>
                       !string.IsNullOrEmpty(m.Url) &&
                       path.StartsWith(m.Url.ToLower())
                   );
                }
                // ✅ Store MenuID and full menulist (for layout)
                if (matchedMenu != null)
                {
                    context.Items["MenuID"] = matchedMenu.MenuID;
                    context.Items["UserMenu"] = menuList;
                }
                else if (matchedMenu == null)
                {
                    context.Response.Redirect("/Home/Error?statusCode=403&errorMessage=Access Denied");
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

        private bool IsRealViewAction(ControllerActionDescriptor action)
        {
            if (action == null) return false;

            var method = action.MethodInfo;

            // 1. Must be inside a Controller (not ControllerBase)
            if (!typeof(Controller).IsAssignableFrom(action.ControllerTypeInfo))
                return false;

            // 2. Must NOT be [HttpPost], [HttpPut], [HttpDelete]
            if (method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()) return false;
            if (method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any()) return false;
            if (method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any()) return false;

            // 3. Must NOT be API-like
            if (method.GetCustomAttributes(typeof(ProducesAttribute), false).Any()) return false;
            if (method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false).Any()) return false;

            // 4. Must NOT be NonAction
            if (method.GetCustomAttributes(typeof(NonActionAttribute), false).Any()) return false;

            // 5. Must NOT return JSON, File, Redirect, or PartialView explicitly
            var rt = method.ReturnType;

            if (typeof(JsonResult).IsAssignableFrom(rt)) return false;
            if (typeof(FileResult).IsAssignableFrom(rt)) return false;
            if (typeof(RedirectResult).IsAssignableFrom(rt)) return false;
            if (typeof(RedirectToActionResult).IsAssignableFrom(rt)) return false;
            if (typeof(PartialViewResult).IsAssignableFrom(rt)) return false;

            // If all checks passed → it is a real View()
            return true;
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
