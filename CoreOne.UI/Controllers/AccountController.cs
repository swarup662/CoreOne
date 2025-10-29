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
            var url = _apiSettings.BaseUrlAuthentication + "/Login";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                // Read message returned from API
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(resultJson);

                TempData["message"] = result?.message ?? "Login failed"; // use API message if available
                TempData["messagetype"] = "error";
                return View(model);
            }

            var resultJsonSuccess = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(resultJsonSuccess);

            var token = data.token.ToString();

            // Store in a session cookie
            HttpContext.Response.Cookies.Append(
                "jwtToken",
                token,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
            TempData["message"] = "Login successfull";
            TempData["messagetype"] = "success";
            return RedirectToAction("UserDashboard", "Home");
        }






        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Cookies["jwtToken"];
            var user = TokenHelper.UserFromToken(HttpContext);

            if (user != null)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var url = _apiSettings.BaseUrlAuthentication + "/Logout";

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
                catch
                {
                    // Ignore API errors
                }
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
            var url = $"{_apiSettings.BaseUrlAuthentication}/ForgotPassword";
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
            var url = $"{_apiSettings.BaseUrlAuthentication}/ValidateResetToken?token={token}";
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
            var url = $"{_apiSettings.BaseUrlAuthentication}/ResetPassword";
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
            var url = $"{_apiSettings.BaseUrlAuthentication}/ChangePassword";
            var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
            var res = await client.PostAsync(url, content);
            var body = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PasswordValidationResponse>(body);
            return Json(result);
        }

        #endregion

    }
}