using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace CoreOne.UI.Controllers
{
    public class MenuLayoutController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        public MenuLayoutController(IHttpClientFactory httpClientFactory, SettingsService settingsService, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        public async Task<List<MenuItem>> UserMenu(HttpContext context)
        {
           

            var menuItems = new List<MenuItem>();
            var userId = TokenHelper.GetUserIdFromToken(context);

            var client = _httpClientFactory.CreateClient();
            var token = context?.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"{_apiSettings.BaseUrlPermission}/GetUserMenu/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                menuItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MenuItem>>(json);
            }


            return menuItems;
        }




    }
}
