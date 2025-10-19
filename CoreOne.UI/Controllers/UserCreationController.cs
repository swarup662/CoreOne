using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http;
using System.Text;

namespace CoreOne.UI.Controllers
{
    public class UserCreationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;
        private readonly ActionPermissionHtmlProcessorUiHelper _htmlProcessor;

        public UserCreationController(IHttpClientFactory httpClientFactory, SettingsService settingsService, ActionPermissionHtmlProcessorUiHelper htmlProcessor)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string search = null, string searchCol = "", string sortColumn = "", string sortDir = null, string status = null)
        {
            var model = new UserCreationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                SearchCol = searchCol,
                SortColumn = sortColumn,
                SortDir = sortDir,
                Status = status
            };

            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetUsers"; // change base url in settings
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Users = new List<UserCreation>();
                ViewBag.TotalRecords = 0;
            }
            else
            {
                var response = await resp.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<UserCreationPagedResponse>(response);

                ViewBag.Users = apiResult?.Items ?? new List<UserCreation>();
                ViewBag.TotalRecords = apiResult?.TotalRecords ?? 0;
            }

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.SearchCol = searchCol;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDir = sortDir;

            var user = TokenHelper.UserFromToken(HttpContext);
            ViewBag.RoleList = await GetRoles(user.UserID);
            ViewBag.GenderList = await GetGenders(user.UserID);
            ViewBag.MailTypeList = await GetMailTypes(user.UserID);

            var ddlresponse = await client.GetAsync($"{_api.BaseUrlRolePermission}/roles");
            if (!ddlresponse.IsSuccessStatusCode)
            {
                ViewBag.Roles = null;
            }
            else
            {
                var ddljson = await ddlresponse.Content.ReadAsStringAsync();
                var ddlroles = JsonConvert.DeserializeObject<IEnumerable<RolePermission>>(ddljson);
                ViewBag.Roles = ddlroles;
            }

            return View();
        }

        [HttpPost]
        public async Task<List<SelectListItem>> GetRoles([FromBody] int userId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetRoles";

            var json = JsonConvert.SerializeObject(userId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var roleList = new List<SelectListItem>();
            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
                return roleList;

            var response = await resp.Content.ReadAsStringAsync();
            var dt = JsonConvert.DeserializeObject<DataTable>(response);

            // Convert DataTable → List<SelectListItem>
            roleList.Add(new SelectListItem
            {
                Value = "",
                Text = "-- Select Role --",
                Selected = true
            });
            foreach (DataRow row in dt.Rows)
            {
                roleList.Add(new SelectListItem
                {
                    Value = row["RoleID"].ToString(),   // column name in API response
                    Text = row["RoleName"].ToString()  // column name in API response
                });
            }
            return roleList;
          
           

           
        }



        [HttpPost]
        public async Task<List<SelectListItem>> GetMailTypes([FromBody] int userId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetMailTypes";

            var json = JsonConvert.SerializeObject(userId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var MailtypeList = new List<SelectListItem>();
            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
                return MailtypeList;

            var response = await resp.Content.ReadAsStringAsync();
            var dt = JsonConvert.DeserializeObject<DataTable>(response);

            // Convert DataTable → List<SelectListItem>
            MailtypeList.Add(new SelectListItem
            {
                Value = "",
                Text = "-- Select Mailtype --",
                Selected = true
            });
            foreach (DataRow row in dt.Rows)
            {
                MailtypeList.Add(new SelectListItem
                {
                    Value = row["MailTypeID"].ToString(),   // column name in API response
                    Text = row["MailTypeName"].ToString()  // column name in API response
                });
            }
            return MailtypeList;




        }


        [HttpPost]
        public async Task<List<SelectListItem>> GetGenders([FromBody] int userId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetGenders";

            var json = JsonConvert.SerializeObject(userId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var GenderList = new List<SelectListItem>();
            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
                return GenderList;

            var response = await resp.Content.ReadAsStringAsync();
            var dt = JsonConvert.DeserializeObject<DataTable>(response);

            // Convert DataTable → List<SelectListItem>
            GenderList.Add(new SelectListItem
            {
                Value = "",
                Text = "-- Select Gender --",
                Selected = true
            });
            foreach (DataRow row in dt.Rows)
            {
                GenderList.Add(new SelectListItem
                {
                    Value = row["GenderID"].ToString(),   // column name in API response
                    Text = row["GenderName"].ToString()  // column name in API response
                });
            }
            return GenderList;




        }

        [HttpPost]
        public async Task<IActionResult> SaveUser([FromBody] UserCreation model)
        {
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
            if (!TryValidateModel(model))
            {
                // Collect validation errors into a dictionary
                var errors = "";
                return Json(new { success = false, errors });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = model.UserID > 0
                    ? _api.BaseUrlUserCreation + "/UpdateUser"
                    : _api.BaseUrlUserCreation + "/AddUser";

                var user = TokenHelper.UserFromToken(HttpContext);
                model.CreatedBy = user.UserID;

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(url, content);

                if (resp.IsSuccessStatusCode)
                {
                    var responseString = await resp.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                    int userId = result.userID;
         
                    if (userId > 0)
                    {
                        return Json(new { success = true, message = "success" });
                    }
                    else if (userId == -2)
                    {
                        return Json(new { success = false, message = "exist" });
                    }
                    else
                    {
                        return Json(new { success = true, message = "error" });

                    }
                }

                return Json(new { success = false, message = "Could not save user. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ValidateField([FromBody] Dictionary<string, string> fieldData)
        {
            var model = new UserCreation(); // Assuming your model class is named 'User'

            // Bind only the incoming single field dynamically
            foreach (var field in fieldData)
            {
                switch (field.Key)
                {
                    case "UserName":
                        model.UserName = field.Value;
                        break;

                    case "Email":
                        model.Email = field.Value;
                        break;

                    case "PhoneNumber":
                        model.PhoneNumber = field.Value;
                        break;

                    case "RoleID":
                        if (int.TryParse(field.Value, out int roleId))
                            model.RoleID = roleId;
                        break;

                    case "GenderID":
                        if (int.TryParse(field.Value, out int genderId))
                            model.GenderID = genderId;
                        break;

                    case "MailTypeID":
                        if (int.TryParse(field.Value, out int mailTypeId))
                            model.MailTypeID = mailTypeId;
                        break;
                }
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

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] int userId)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/DeleteUser";

            var payload = new UserCreation { UserID = userId, CreatedBy = user.UserID };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);

            return Json(new { success = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> GetUserById([FromBody] int userId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetUserById";

            var json = JsonConvert.SerializeObject(userId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
                return BadRequest();

            var response = await resp.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserCreation>(response);

            return Json(user);
        }

        // =========================
        // Get permissions for modal (AJAX)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetRoleAndExtraPermissions(int userId, int roleId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_api.BaseUrlRolePermission}/GetRolePermissionByRoleId/{roleId}");
            var json = await response.Content.ReadAsStringAsync();
            var permissions = JsonConvert.DeserializeObject<IEnumerable<RolePermission>>(json);

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
    }
}
