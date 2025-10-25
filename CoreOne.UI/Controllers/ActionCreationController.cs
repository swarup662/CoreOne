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


        [HttpPost]
        public async Task<IActionResult> GetActionById([FromBody] int actionId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlActionCreation + "/GetActionById";

            var json = JsonConvert.SerializeObject(actionId); // plain integer JSON
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
                return BadRequest();

            var response = await resp.Content.ReadAsStringAsync();
            var action = JsonConvert.DeserializeObject<ActionCreationDto>(response);

            return Json(action);
        }





    }
}
