using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CoreOne.UI.Controllers
{
    public class ModuleSetupController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;
        private readonly ActionPermissionHtmlProcessorUiHelper _htmlProcessor;
        public ModuleSetupController(IHttpClientFactory httpClientFactory, SettingsService settingsService, ActionPermissionHtmlProcessorUiHelper htmlProcessor)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
        }

        [HttpGet]
        public async Task<IActionResult> Index( int pageNumber = 1, int pageSize = 10, string search = null,  string searchCol = "", string sortColumn = "", string sortDir = null)
        {
            // Prepare the request model
            var model = new RoleCreationsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                SearchCol = searchCol, // optional, kept for compatibility
                SortColumn = sortColumn,
                SortDir = sortDir
            };

            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlModuleSetup + "/GetMenuModule"; // call your ModuleSetup API
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call API
            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.MenuModules = new List<MenuModuleDto>();
                ViewBag.TotalRecords = 0;
            }
            else
            {
                var response = await resp.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<MenuModulePagedResponse>(response);

                ViewBag.MenuModules = apiResult?.Items ?? new List<MenuModuleDto>();
                ViewBag.TotalRecords = apiResult?.TotalRecords ?? 0;
            }

            // Set ViewBag for UI
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.SearchCol = searchCol;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDir = sortDir;

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> SaveMenuWithModules([FromBody] MenuWithModulesSave model)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            model.CreatedBy = user.UserID;

            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlModuleSetup + "/SaveMenuWithModules";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode) return Json(new { success = false });

            var response = await resp.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(response);

            return Json(new { success = true, menuId = (int)result.menuId });
        }

        [HttpPost]
        public IActionResult ValidateMenuField([FromBody] Dictionary<string, string> fieldData)
        {
            // Create a dummy model just for validation
            var model = new MenuWithModulesSave();

            // Bind the single field into model
            foreach (var field in fieldData)
            {
                if (field.Key == "MenuName")
                    model.Name = field.Value;
                if (field.Key == "MenuSeq" && int.TryParse(field.Value, out int seq))
                    model.Sequence = seq;
                if (field.Key == "MenuSymbol")
                    model.MenuSymbol = field.Value;
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

        [HttpGet]
        public async Task<IActionResult> GetMenuForEdit(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid menuId" });

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = _api.BaseUrlModuleSetup + $"/GetMenuModuleById/{id}";
                var resp = await client.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorJson = await resp.Content.ReadAsStringAsync();
                    return StatusCode((int)resp.StatusCode, new { message = errorJson });
                }

                var json = await resp.Content.ReadAsStringAsync();

                // ✅ ensure camelCase serialization for front-end
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json, options);

                return Ok(obj);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }




    }
}
