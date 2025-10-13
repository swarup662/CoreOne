using CoreOne.COMMON.Models;
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
    public class MenuModuleController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettings _api;
        private readonly PermissionHtmlProcessor _htmlProcessor;
        public MenuModuleController(IHttpClientFactory httpClientFactory, SettingsService settingsService, PermissionHtmlProcessor htmlProcessor)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
        }

        [HttpGet]
        public async Task<IActionResult> Index( int pageNumber = 1, int pageSize = 10, string search = null,  string searchCol = "", string sortColumn = "", string sortDir = null)
        {
            // Prepare the request model
            var model = new RolesRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                SearchCol = searchCol, // optional, kept for compatibility
                SortColumn = sortColumn,
                SortDir = sortDir
            };

            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlMenuModule + "/GetMenuModule"; // call your MenuModule API
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
            var url = _api.BaseUrlMenuModule + "/SaveMenuWithModules";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode) return Json(new { success = false });

            var response = await resp.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(response);

            return Json(new { success = true, menuId = (int)result.menuId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuForEdit(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid menuId" });

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = _api.BaseUrlMenuModule + $"/GetMenuModuleById/{id}";
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
