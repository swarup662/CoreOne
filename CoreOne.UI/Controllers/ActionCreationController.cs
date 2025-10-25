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



        [HttpPost]
        public async Task<IActionResult> SaveAction([FromBody] ActionCreationDto model)
        {
            if (!TryValidateModel(model))
            {
                // Collect validation errors into a dictionary
                var errors = "";
                return Json(new { success = false, errors });
            }

            try
            {
                if (model.ActionID == 0) model.ActionID = 0;

                var client = _httpClientFactory.CreateClient();
                var url = model.ActionID > 0 ? _api.BaseUrlActionCreation + "/UpdateAction" : _api.BaseUrlActionCreation + "/AddAction";
                var user = TokenHelper.UserFromToken(HttpContext);

                model.CreatedBy = user.UserID;

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(url, content);

                if (resp.IsSuccessStatusCode)
                {
                    var responseString = await resp.Content.ReadAsStringAsync();

                    // Deserialize to dynamic object
                    var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                    int actionID = result.actionID;
                    string message = result.message;

                    if (actionID > 0)
                    {
                        return Json(new { success = true, message = "success" });
                    }
                    else if (actionID == -2)
                    {
                        return Json(new { success = false, message = "exist" });
                    }
                    else
                    {
                        return Json(new { success = true, message = "error" });

                    }
                }

                return Json(new { success = false, message = "Could not save Action. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult ValidateField([FromBody] Dictionary<string, string> fieldData)
        {
            var model = new ActionCreationDto();

            // Bind the single field into model
            foreach (var field in fieldData)
            {
                if (field.Key == "ActionName")
                    model.ActionName = field.Value;
                if (field.Key == "Description")
                    model.Description = field.Value;
            }

            // Validate only that property
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { errors });
            }

            return Json(new { success = true });
        }




    }
}
