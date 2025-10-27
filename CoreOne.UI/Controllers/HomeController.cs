using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using CoreOne.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace CoreOne.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;
        private readonly ActionPermissionHtmlProcessorUiHelper _htmlProcessor;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, SettingsService settingsService, ActionPermissionHtmlProcessorUiHelper htmlProcessor)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AdminDashboard()
        {
            return View();
        }
        public IActionResult UserDashboard()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> MyAccount()
        {
            var client = _httpClientFactory.CreateClient();
            var loggedUser = TokenHelper.UserFromToken(HttpContext);
            int userId = loggedUser.UserID;

            var url = _api.BaseUrlUserCreation + "/GetUserMyAccount";

            var json = JsonConvert.SerializeObject(userId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);

            if (!resp.IsSuccessStatusCode)
                return BadRequest("API call failed");

            var response = await resp.Content.ReadAsStringAsync();
            var userData = JsonConvert.DeserializeObject<UserCreation>(response);

            return View(userData);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode, string errorMessage)
        {
            int finalStatusCode = statusCode ?? 500;
            string finalErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Internal server error" : errorMessage;

            ViewBag.StatusCode = finalStatusCode;
            ViewBag.ErrorMessage = finalErrorMessage;

            return View();
        }
    }
}
