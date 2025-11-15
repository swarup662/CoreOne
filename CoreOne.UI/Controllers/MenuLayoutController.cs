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
        private readonly SignedCookieHelper _cookieHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MenuLayoutController(IHttpClientFactory httpClientFactory, SettingsService settingsService, IConfiguration config, IHttpContextAccessor httpContextAccessor, SignedCookieHelper cookieHelper)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
            _cookieHelper = cookieHelper;
            _httpContextAccessor = httpContextAccessor;

        }

        public async Task<List<MenuItem>> UserMenu(HttpContext context)
        {
           

            var menuItems = new List<MenuItem>();
            var user = TokenHelper.UserFromToken(_httpContextAccessor.HttpContext, _cookieHelper);
            var userId = TokenHelper.GetUserIdFromToken(context);

            var client = _httpClientFactory.CreateClient();
            var token = context?.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"{_apiSettings.BaseUrlPermission}/GetUserMenu/{userId}/{user.CurrentApplicationID}/{user.CurrentCompanyID}/{user.CurrentRoleID}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                menuItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MenuItem>>(json);
            }


            return menuItems;
        }




    }
}
