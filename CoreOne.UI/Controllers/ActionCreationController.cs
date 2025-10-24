using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoreOne.UI.Controllers
{
    public class ActionCreationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;
        private readonly ActionPermissionHtmlProcessorUiHelper _htmlProcessor;
        public ActionCreationController(IHttpClientFactory httpClientFactory, SettingsService settingsService, ActionPermissionHtmlProcessorUiHelper htmlProcessor)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
        }



        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = _api.BaseUrlActionCreation + "/GetActions";

                var resp = await client.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    ViewBag.Actions = new List<ActionCreationDto>();
                    ViewBag.Error = "Failed to fetch data from API.";
                    return View();
                }

                var json = await resp.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<ActionCreationDto>>(json);

                ViewBag.Actions = result;
            }
            catch (Exception ex)
            {
                ViewBag.Actions = new List<ActionCreationDto>();
                ViewBag.Error = "Error: " + ex.Message;
            }

            return View();
        }








    }
}
