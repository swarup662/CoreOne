using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly EncryptionHelper EncryptionHelper;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(IHttpClientFactory factory, IConfiguration config, SignedCookieHelper cookie, IHttpContextAccessor httpContextAccessor, EncryptionHelper encryptionHelper)
        {
            _httpClientFactory = factory;
            _config = config;
            _cookie = cookie;
            _httpContextAccessor = httpContextAccessor;
            EncryptionHelper = encryptionHelper;
        }


        [HttpGet("consume")]
        public async Task<IActionResult> Consume([FromQuery] ConsumeInputModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.OAuth))
                return BadRequest("Invalid input model.");

            var client = _httpClientFactory.CreateClient();
            string authApi = _config["AuthApi:BaseUrl"];
            string appSecret = _config["App:Secret"];
            string appId = _config["App:Id"];

            var requestBody = new
            {
                CacheKey = model.OAuth,
                ApplicationID = int.Parse(appId),
                AppSecret = appSecret
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{authApi}/exchange-cachekey", content);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return Unauthorized($"Token exchange failed: {msg}");
            }

            var data = JObject.Parse(await response.Content.ReadAsStringAsync());
            string token = data["access_token"].ToString();

            // Save JWT token
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(1)
            });

            // Build UserCtx
            string rawContext = $"{model.CompanyID}|{model.ApplicationID}|{model.RoleID}";
            string signed = _cookie.CreateSignedValue(rawContext);

            Response.Cookies.Append("UserCtx", signed, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(1)
            });

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

        [HttpPost("SetUserContext")]
        public IActionResult SetUserContext(
    [FromBody] ContextRequest body,
    [FromServices] SignedCookieHelper cookieHelper)
        {
            try
            {
                if (body == null || string.IsNullOrEmpty(body.Context))
                    return Json(new { success = false, message = "Missing context" });

                string signed = cookieHelper.CreateSignedValue(body.Context);

                Response.Cookies.Append("UserCtx", signed, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public class ContextRequest
        {
            public string Context { get; set; }
        }

        [HttpGet("GetCurrentContext")]
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
