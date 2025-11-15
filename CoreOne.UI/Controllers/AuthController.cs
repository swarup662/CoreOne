using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreOne.App.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly SignedCookieHelper _cookie;
 
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(IHttpClientFactory factory, IConfiguration config, SignedCookieHelper cookie, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = factory;
            _config = config;
            _cookie = cookie;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("consume")]
        public async Task<IActionResult> Consume([FromQuery] string OAuth)
        {
            if (string.IsNullOrEmpty(OAuth))
                return BadRequest("Missing cache key");

            var client = _httpClientFactory.CreateClient();
            string authApi = _config["AuthApi:BaseUrl"];
            string appSecret = _config["App:Secret"];
            string appId = _config["App:Id"];

            var requestBody = new
            {
                CacheKey = OAuth,
                ApplicationID = int.Parse(appId),
                AppSecret = appSecret
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{authApi}/exchange-cachekey", content);
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return Unauthorized($"Token exchange failed: {msg}");
            }

            var data = JObject.Parse(await response.Content.ReadAsStringAsync());
            string token = data["access_token"].ToString();

            // Save token in cookie (or session)
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Redirect to dashboard
            return Redirect("/Home/UserDashboard");
        }

        [HttpGet("GetSwitchOptions")]
        public IActionResult GetSwitchOptions(
            [FromServices] IHttpContextAccessor accessor,
            [FromServices] SignedCookieHelper cookieHelper)
        {
            var user = TokenHelper.UserFromToken(_httpContextAccessor.HttpContext, cookieHelper);
            if (user == null)
                return Json(new List<object>());

            var access = user.UserAccessList ?? new List<UserAccessViewModel>();

            var result = access.Select(a => new
            {
                companyID = a.CompanyID,
                companyName = a.CompanyName,
                applicationID = a.ApplicationID,
                applicationName = a.ApplicationName,
                roleID = a.RoleID,
                roleName = a.RoleName,
                colorCode = a.ColorCode,
                icon = a.Icon
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public IActionResult SetUserContext([FromBody] dynamic body, [FromServices] SignedCookieHelper cookieHelper)
        {
            try
            {
                string value = (string)body.context;   // "company|app|role"
                string signed = cookieHelper.CreateSignedValue(value);

                Response.Cookies.Append("UserCtx", signed, new CookieOptions
                {
                    HttpOnly = true,         // ⭐ Cannot be read by JS
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8)
                });

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false, message = "Failed" });
            }
        }
        [HttpGet]
        public IActionResult GetCurrentContext([FromServices] SignedCookieHelper cookieHelper)
        {
            // Read secure signed cookie
            if (!Request.Cookies.TryGetValue("UserCtx", out string? signed))
            {
                return Json(new
                {
                    success = false,
                    message = "No active context found"
                });
            }

            // Validate + decrypt signed cookie
            var raw = cookieHelper.ValidateAndGet(signed);

            if (raw == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid or tampered cookie"
                });
            }

            // raw = "CompanyID|ApplicationID|RoleID"
            var parts = raw.Split('|');
            if (parts.Length != 3)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid context format"
                });
            }

            return Json(new
            {
                success = true,
                companyId = int.Parse(parts[0]),
                applicationId = int.Parse(parts[1]),
                roleId = int.Parse(parts[2])
            });
        }




    }
}
