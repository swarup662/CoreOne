using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.IdentityModel.Tokens.Jwt;
using CoreOne.UI.Helper;
using CoreOne.UI.Service;
using CoreOne.DOMAIN.Models;
using Newtonsoft.Json;
using System.Text;

namespace CoreOne.UI.Filters
{
    public class AuthorizationFilter : IActionFilter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        public AuthorizationFilter(IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var path = http.Request.Path.Value?.Split('?')[0].ToLower() ?? "";
            var token = http.Request.Cookies["jwtToken"];

            // -----------------------------------
            // STEP 1: Redirect root "/" → Login
            // -----------------------------------
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                context.Result = new RedirectResult("/Account/Login");
                return;
            }

            // -----------------------------------
            // STEP 2: Skip public URLs
            // -----------------------------------
            var skipPaths = new[]
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
                "/notification/markasread"
            };

            if (skipPaths.Any(p => path.StartsWith(p)))
                return;

            // -----------------------------------
            // STEP 3: Validate Token
            // -----------------------------------
            if (string.IsNullOrEmpty(token) || !TokenHelper.IsTokenValid(http))
            {
                context.Result = new RedirectResult("/Account/Login");
                return;
            }

            // -----------------------------------
            // STEP 4: Detect Action
            // -----------------------------------
            var action = context.ActionDescriptor as ControllerActionDescriptor;
            if (action == null) return;  // Not MVC

            // API controller → allow
            bool isApi = action.ControllerTypeInfo
                .GetCustomAttributes(typeof(ApiControllerAttribute), true)
                .Any();

            if (isApi) return;

            // View detection
            bool isViewAction = IsViewAction(action);

            // Non-view (JSON, Redirect, File, POST) → allow
            if (!isViewAction) return;

            // -----------------------------------
            // STEP 5: Allow default views
            // -----------------------------------
            string[] alwaysAllowedViews =
            {
                "/dashboard",
                "/profile",
                "/home/index",
                "/error",
                "/home/error"
            };

            if (alwaysAllowedViews.Any(v => path.StartsWith(v)))
                return;

            // -----------------------------------
            // STEP 6: Menu Authorization
            // -----------------------------------
            var menuService = http.RequestServices.GetRequiredService<IMenuService>();
            var menuList = menuService.GetUserMenu(http).Result;

            if (menuList == null || menuList.Count == 0)
            {
                context.Result = new RedirectResult("/Home/Error?statusCode=403&errorMessage=Access Denied");
                return;
            }

            var matchedMenu = menuList.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.Url) &&
                path.StartsWith(m.Url.ToLower())
            );

            if (matchedMenu == null)
            {
                context.Result = new RedirectResult("/Home/Error?statusCode=403&errorMessage=Access Denied");
                return;
            }

            // Store for layout
            http.Items["MenuID"] = matchedMenu.MenuID;
            http.Items["UserMenu"] = menuList;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        // ---------------------
        // VIEW ACTION DETECTOR
        // ---------------------
        private bool IsViewAction(ControllerActionDescriptor action)
        {
            var method = action.MethodInfo;
            var returnType = method.ReturnType;

            // Not inside normal Controller
            if (!typeof(Controller).IsAssignableFrom(action.ControllerTypeInfo))
                return false;

            // POST/PUT/DELETE are NOT views
            if (method.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()) return false;
            if (method.GetCustomAttributes(typeof(HttpPutAttribute), false).Any()) return false;
            if (method.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any()) return false;

            // NonAction
            if (method.GetCustomAttributes(typeof(NonActionAttribute), false).Any()) return false;

            // JSON / File / Redirect / Partial are NOT views
            if (typeof(JsonResult).IsAssignableFrom(returnType)) return false;
            if (typeof(FileResult).IsAssignableFrom(returnType)) return false;
            if (typeof(RedirectResult).IsAssignableFrom(returnType)) return false;
            if (typeof(RedirectToRouteResult).IsAssignableFrom(returnType)) return false;
            if (typeof(PartialViewResult).IsAssignableFrom(returnType)) return false;

            // Default: This is a View()
            return true;
        }
    }
}
