using CoreOne.UI.Helper;
using CoreOne.UI.Service;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CoreOne.UI.Middleware.Filter
{
    public class AuthorizationFilter : IAsyncActionFilter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        // single skip list used by both pre and top-level checks
        private static readonly string[] SkipPaths = new[]
        {
            "/account/login",
            "/account/logout",
            "/account/forgotpassword",
            "/account/resetpassword",
            "/account/companyselection",
            "/account/getapplicationsbycompany",
            "/account/getcompanies",
            "/account/urlexistsasync",
            "/account/launchapp",
            "/account/validatecompanyselection",
            "/account/companylogout",
            "/account/cleartempdata",

            "/api/fileupload/viewpost",

            "/auth/consume",
            "/auth/getswitchoptions",
            "/auth/setusercontext",
            "/auth/getcurrentcontext",

            "/notification/clearall",
            "/notification/markasread",
            "/home/error"
        };

        public AuthorizationFilter(IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var path = http.Request.Path.Value?.Split('?')[0].ToLower() ?? "";
            var token = http.Request.Cookies["jwtToken"];

            // If this is a public/skip path -> just continue
            if (SkipPaths.Any(p => path.StartsWith(p)))
            {
                await next();
                return;
            }

            // Pre-authorize checks (root, endpoint exists, token)
            if (!PreAuthorize(context))
            {
                // PreAuthorize already set context.Result when needed
                return;
            }

            // Execute action
            var executedContext = await next();

            // Inspect actual action result (post-action)
            var result = executedContext.Result;

            // Only enforce menu authorization for full ViewResult
            if (result is ViewResult)
            {
                var authorized = await AuthorizeMenuAsync(http, path, token);
                if (!authorized)
                {
                    executedContext.Result = new RedirectResult("/Home/Error?statusCode=403&errorMessage=Access Denied");
                }
            }
            // else: ContentResult, PartialViewResult, JsonResult, FileResult, RedirectResult etc. are allowed
        }

        // ---------------------------
        // Pre-authorization checks (before action executes)
        // ---------------------------
        private bool PreAuthorize(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var path = http.Request.Path.Value?.Split('?')[0].ToLower() ?? "";
            var token = http.Request.Cookies["jwtToken"];

            // Root -> redirect to login
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                context.Result = new RedirectResult("/Account/Login");
                return false;
            }

            // Wrong route -> 404
            if (http.GetEndpoint() == null)
            {
                context.Result = new RedirectResult("/Home/Error?statusCode=404&errorMessage=Page not found");
                return false;
            }

            // Skip public paths (defensive - already checked above, but harmless)
            if (SkipPaths.Any(p => path.StartsWith(p)))
                return true;

            // Token check: if no token or invalid -> redirect to login
            if (string.IsNullOrEmpty(token) || !TokenHelper.IsTokenValid(http))
            {
                context.Result = new RedirectResult("/Account/Login");
                return false;
            }

            return true;
        }

        // ---------------------------
        // Run menu authorization (only for ViewResult). Async and non-blocking.
        // ---------------------------
        private async Task<bool> AuthorizeMenuAsync(HttpContext http, string path, string token)
        {
            // Token must be present and valid
            if (string.IsNullOrEmpty(token) || !TokenHelper.IsTokenValid(http))
                return false;

            // Views always allowed (no menu check)
            string[] alwaysAllowed = { "/dashboard", "/profile", "/home/index", "/error", "/home/error" };
            if (alwaysAllowed.Any(a => path.StartsWith(a)))
                return true;

            // Resolve the service and call asynchronously
            var menuService = http.RequestServices.GetRequiredService<IMenuService>();
            var menuList = await menuService.GetUserMenu(http);

            if (menuList == null || menuList.Count == 0)
                return false;

            var matched = menuList.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.Url) &&
                path.StartsWith(m.Url.ToLower())
            );

            if (matched != null)
            {
                http.Items["MenuID"] = matched.MenuID;
                http.Items["UserMenu"] = menuList;
                return true;
            }

            return false;
        }
    }
}
