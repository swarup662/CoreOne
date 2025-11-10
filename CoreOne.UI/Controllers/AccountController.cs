using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using static System.Net.WebRequestMethods;
namespace CoreOne.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly string BaseUrlAuth;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        public AccountController(IConfiguration config, IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {

            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;

        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/login";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(resultJson);
                TempData["message"] = result?.message ?? "Login failed";
                TempData["messagetype"] = "error";
                return View(model);
            }

            var data = JsonConvert.DeserializeObject<dynamic>(resultJson);

            var token = (string)data.token;
            Boolean IsInternal = false;
             IsInternal = data.user.isInternal;
            if (!IsInternal) { 
            // Store token in cookie
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            // External user (redirectUrl returned directly)
            if (data.redirectUrl != null)
                return Redirect(data.redirectUrl.ToString());
              }
            // Internal user → show company selection
            string accessListJson = data.accessList.ToString();

            string userId = data.user?.userID?.ToString() ?? data.userID?.ToString();
            TempData["accessList"] = accessListJson;
            TempData["userId"] = data.user?.userID?.ToString() ?? data.userID?.ToString();

            return RedirectToAction("CompanySelection", "Account");
        }

        [HttpGet]
        public IActionResult CompanySelection()
        {
            if (TempData["accessList"] == null)
                return RedirectToAction("Login");

            var accessList = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(TempData["accessList"].ToString());

            // Re-stash access list so it survives page reload
            TempData.Keep("accessList");
            TempData.Keep("userId");

            return View(accessList);
        }
        [HttpPost]
        public async Task<IActionResult> LaunchApp([FromBody] AppLaunchRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/create-cachekey";

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = JsonConvert.DeserializeObject<dynamic>(resultJson);
                return Json(new { success = false, message = err?.message ?? "Error creating cache key" });
            }

            var data = JsonConvert.DeserializeObject<dynamic>(resultJson);

            string redirectUrl = data.redirectUrl.ToString();

            return Json(new { success = true, redirectUrl });
        }

        [HttpGet]
        public IActionResult GetCompanies()
        {
            if (TempData["accessList"] == null)
                return Json(new List<object>());

            var accessList = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(TempData["accessList"].ToString());
            TempData.Keep("accessList");

            var companies = accessList
                .GroupBy(a => new { a.CompanyID, a.CompanyName })
                .Select(g => new { g.Key.CompanyID, g.Key.CompanyName })
                .ToList();

            return Json(companies);
        }

        [HttpGet]
        public IActionResult GetApplicationsByCompany(int companyId)
        {
            if (TempData["accessList"] == null)
                return Json(new List<object>());

            var accessList = JsonConvert.DeserializeObject<List<UserAccessViewModel>>(TempData["accessList"].ToString());
            TempData.Keep("accessList");

            var apps = accessList
                .Where(a => a.CompanyID == companyId)
                .Select(a => new
                {
                    a.ApplicationID,
                    a.ApplicationName,
                    a.RoleID,
                    a.RoleName,
                    a.ColorCode, // ✅ New field
                    a.Icon     // ✅ New field
                })
                .ToList();

            return Json(apps);
        }


        //[HttpPost]
        //public async Task<IActionResult> Logout()
        //{
        //    var token = Request.Cookies["jwtToken"];
        //    Response.Cookies.Delete("jwtToken");

        //    if (token != null)
        //    {
        //        try
        //        {
        //            var client = _httpClientFactory.CreateClient();
        //            var url = $"{_apiSettings.BaseUrlAuth}/api/v1/Auth/logout";

        //            var request = new HttpRequestMessage(HttpMethod.Post, url);
        //            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //            await client.SendAsync(request);
        //        }
        //        catch { /* ignore errors */ }
        //    }

        //    return RedirectToAction("Login", "Account");
        //}




        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Cookies["jwtToken"];
            var user = TokenHelper.UserFromToken(HttpContext);

            if (user != null)
            {
                
                    var client = _httpClientFactory.CreateClient();
                    var url = _apiSettings.BaseUrlAuth + "/Logout";

                    var json = JsonConvert.SerializeObject(user.UserID);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = content
                    };

                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    await client.SendAsync(request);
                
            }

            // Clear cookie
            HttpContext.Response.Cookies.Delete("jwtToken");

            return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
        }

        [HttpPost]

        public IActionResult ClearTempData()
        {
            TempData["message"] = null;
            return Ok();
        }


        #region change-forgot-password

        public IActionResult ForgotPassword() => View();
        public IActionResult ResetPassword(string token) => View(model: token);
        public IActionResult ChangePassword() => View();
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/ForgotPassword";
            var content = new StringContent(JsonConvert.SerializeObject(email), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var body = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PasswordValidationResponse>(body);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> ValidateResetToken(string token)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/ValidateResetToken?token={token}";
            var res = await client.GetAsync(url);

            if (!res.IsSuccessStatusCode)
                return Json(new PasswordValidationResponse { Success = false, Message = "API call failed" });

            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PasswordValidationResponse>(json);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
         

            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/ResetPassword";
            var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var body = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PasswordValidationResponse>(body);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            req.UserID = user.UserID;

            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiSettings.BaseUrlAuth}/ChangePassword";
            var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var body = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PasswordValidationResponse>(body);
            return Json(result);
        }

        #endregion

    }
}