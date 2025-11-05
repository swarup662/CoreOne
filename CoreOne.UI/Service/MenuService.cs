using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace CoreOne.UI.Service
{
    public class MenuService : IMenuService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _apiSettings;

        public MenuService(IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        public async Task<List<MenuItem>> GetUserMenu(HttpContext context)
        {
            var menuItems = new List<MenuItem>();

            var userId = TokenHelper.GetUserIdFromToken(context);
            var token = context.Request.Cookies["jwtToken"];

            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"{_apiSettings.BaseUrlPermission}/GetUserMenu/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                menuItems = JsonConvert.DeserializeObject<List<MenuItem>>(json);
            }

            return menuItems;
        }
    }
}