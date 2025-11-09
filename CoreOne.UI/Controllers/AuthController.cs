using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CoreOne.App.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AuthController(IHttpClientFactory factory, IConfiguration config)
        {
            _httpClientFactory = factory;
            _config = config;
        }

        [HttpGet("consume")]
        public async Task<IActionResult> Consume([FromQuery] string ck)
        {
            if (string.IsNullOrEmpty(ck))
                return BadRequest("Missing cache key");

            var client = _httpClientFactory.CreateClient();
            string authApi = _config["AuthApi:BaseUrl"];
            string appSecret = _config["App:Secret"];
            string appId = _config["App:Id"];

            var requestBody = new
            {
                CacheKey = ck,
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
    }
}
