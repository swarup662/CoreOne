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
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly SignedCookieHelper _cookieHelper;

        public MenuService(IHttpClientFactory httpClientFactory, SettingsService settingsService, SignedCookieHelper cookieHelper, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
            _cookieHelper= cookieHelper;
            _httpContextAccessor= httpContextAccessor;
        }

        public async Task<List<MenuItem>> GetUserMenu(HttpContext context)
        {
            var menuItems = new List<MenuItem>();

            var user = TokenHelper.UserFromToken(_httpContextAccessor.HttpContext, _cookieHelper);
            var token = context.Request.Cookies["jwtToken"];

            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"{_apiSettings.BaseUrlPermission}/GetUserMenu/{user.UserID}/{user.CurrentApplicationID}/{user.CurrentCompanyID}/{user.CurrentRoleID}");



            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                menuItems = JsonConvert.DeserializeObject<List<MenuItem>>(json);
            }

            return menuItems;
        }
    }
}