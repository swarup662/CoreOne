using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using CoreOne.COMMON.Models;
using CoreOne.UI.Helper;
using System.Data;
using System.Collections.Generic;

namespace CoreOne.UI.Controllers
{
    public class RolePermissionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettings _apiSettings;

        public RolePermissionController(IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _apiSettings = settingsService.ApiSettings;
        }

        // =========================
        // Grid
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string search = null, string sortColumn = "", string sortDir = null)
        {
            var model = new RoleGridRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                SortColumn = sortColumn,
                SortDir = sortDir
            };

            var client = _httpClientFactory.CreateClient();
            var url = _apiSettings.BaseUrlRolePermission + "/GetRoleGrid";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                List<RolePermissionModel> erroles = new List<RolePermissionModel>();
                return View(erroles);
                ViewBag.Roles = null; // 👈 kept as is
            }
            else
            {
                var response = await resp.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<RoleGridPagedResponse>(response);

                var roles = apiResult?.Items ?? new List<RolePermissionModel>();
                ViewBag.RolePermissions = roles;
               var ddlresponse = await client.GetAsync($"{_apiSettings.BaseUrlRolePermission}/roles");
                if (!ddlresponse.IsSuccessStatusCode)
                {
                    ViewBag.Roles = null;
                }
                else
                {
                    var ddljson = await ddlresponse.Content.ReadAsStringAsync();
                    var ddlroles = JsonConvert.DeserializeObject<IEnumerable<RolePermissionModel>>(ddljson);
                    ViewBag.Roles = ddlroles;
                }

                // Paging related info
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.Search = search;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDir = sortDir;
                ViewBag.TotalRecords = apiResult?.TotalRecords ?? 0;


                return View();
            }
        }


        // =========================
        // Get permissions for modal (AJAX)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_apiSettings.BaseUrlRolePermission}/GetRolePermissionByRoleId/{roleId}");
            var json = await response.Content.ReadAsStringAsync();
            var permissions = JsonConvert.DeserializeObject<IEnumerable<RolePermissionModel>>(json);

            if (permissions == null || !permissions.Any())
            {
                return Content("<p class='text-danger'>No permissions found.</p>", "text/html");
            }

            // ✅ build HTML string here (same accordion design you had in JS)
            var grouped = permissions
                .GroupBy(p => p.ParentMenuName)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(p => p.ModuleName)
                          .ToDictionary(m => m.Key, m => m.ToList())
                );

            var sb = new System.Text.StringBuilder();
            int menuIndex = 0;

            foreach (var parent in grouped)
            {
                menuIndex++;
                sb.Append($@"
            <div class='accordion-item border mb-3 rounded shadow-sm'>
                <h2 class='accordion-header' id='heading{menuIndex}'>
                    <button class='accordion-button collapsed fw-bold text-dark' type='button' 
                            data-bs-toggle='collapse' data-bs-target='#collapse{menuIndex}'>
                        <i class='bi bi-folder-fill text-primary me-2'></i> {parent.Key ?? "Uncategorized"}
                    </button>
                </h2>
                <div id='collapse{menuIndex}' class='accordion-collapse collapse' data-bs-parent='#permissionsContainer'>
                    <div class='accordion-body bg-light'>");

                foreach (var module in parent.Value)
                {
                    var allChecked = module.Value.All(p => p.HasPermission);
                    sb.Append($@"
                <div class='card border-0 shadow-sm mb-3'>
                    <div class='card-header d-flex justify-content-between align-items-center bg-white'>
                        <strong class='text-secondary'><i class='bi bi-box me-2'></i>{module.Key}</strong>
                        <div class='form-check form-switch'>
                            <input class='form-check-input select-all' type='checkbox' data-module='{module.Key}' {(allChecked ? "checked" : "")}>
                            <label class='form-check-label fw-semibold text-primary'>Select All</label>
                        </div>
                    </div>
                    <div class='card-body pt-3'>
                        <div class='row g-2'>");

                    foreach (var p in module.Value)
                    {
                        sb.Append($@"
                    <div class='col-md-2 col-sm-4 col-6'>
                        <div class='form-check form-switch'>
                            <input class='form-check-input perm-checkbox' type='checkbox'
                                   data-menuid='{p.MenuModuleID}' data-actionid='{p.ActionID}' data-module='{module.Key}'
                                   id='chk_{p.MenuModuleID}_{p.ActionID}' {(p.HasPermission ? "checked" : "")}>
                            <label class='form-check-label small text-dark fw-light' for='chk_{p.MenuModuleID}_{p.ActionID}'>
                                {p.ActionName}
                            </label>
                        </div>
                    </div>");
                    }

                    sb.Append("</div></div></div>");
                }

                sb.Append("</div></div></div>");
            }

            return Content(sb.ToString(), "text/html");
        }

        // =========================
        // Save / Update permissions
        // =========================
        [HttpPost]
        public async Task<IActionResult> SavePermissions(int roleId,  [FromBody] List<RolePermissionModel> permissions)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(permissions), Encoding.UTF8, "application/json");
            var user = TokenHelper.UserFromToken(HttpContext);
            // assuming userId = 1 for demo
            var response = await client.PostAsync($"{_apiSettings.BaseUrlRolePermission}/SaveRolePermission/{roleId}/{user.UserID}", content);
            if (!response.IsSuccessStatusCode)
            {
                return Json("error");
            }
            else
            {
                var json = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<int>(json);
                return Json(res);
            }
               
        }

        // =========================
        // Delete permissions
        // =========================
        [HttpDelete]
        public async Task<IActionResult> DeleteRolePermission(int roleId)
        {
            var client = _httpClientFactory.CreateClient();
            var user = TokenHelper.UserFromToken(HttpContext);
            var response = await client.DeleteAsync($"{_apiSettings.BaseUrlRolePermission}/DeleteRolePermission/{roleId}/{user.UserID}");

            if (!response.IsSuccessStatusCode)
            {
                return Json("error");
            }
            else
            {
                var json = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<int>(json);
                return Json(res);
            }
        }
    }
}
