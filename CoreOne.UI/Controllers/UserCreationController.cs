using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using System;
using System.Text.RegularExpressions;
namespace CoreOne.UI.Controllers
{
    public class UserCreationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;
        private readonly ActionPermissionHtmlProcessorUiHelper _htmlProcessor;
        private readonly NotificationHelper _notificationHelper; // ✅ Add helper

        public UserCreationController(
            IHttpClientFactory httpClientFactory,
            SettingsService settingsService,
            ActionPermissionHtmlProcessorUiHelper htmlProcessor,
            NotificationHelper notificationHelper) // ✅ Inject helper
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
            _htmlProcessor = htmlProcessor;
            _notificationHelper = notificationHelper; // ✅ Assign
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
            ViewBag.NotificationList = await GetNotficationDropdown();
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

        [HttpGet]
        public async Task<List<SelectListItem>> GetNotficationDropdown()
        {
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/GetNotficationDropdown";

            var dropdownList = new List<SelectListItem>();
            var resp = await client.GetAsync(url);

            if (!resp.IsSuccessStatusCode)
                return dropdownList;

            var response = await resp.Content.ReadAsStringAsync();

            // Deserialize directly to your JSON structure
            var notifications = JsonConvert.DeserializeObject<List<Notification_Dropdown>>(response);

            // Add default placeholder
            dropdownList.Add(new SelectListItem
            {
                Value = "",
                Text = "-- Select Notification --",
                Selected = true
            });

            // Convert list to dropdown items
            foreach (var item in notifications)
            {
                dropdownList.Add(new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = item.Name
                });
            }

            return dropdownList;
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

        public  async Task< UserCreationEditDTO >ToEditDTO(UserCreation user)
        {
            return new UserCreationEditDTO
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                MailTypeID = user.MailTypeID,
                PhoneNumber = user.PhoneNumber,
                RoleID = user.RoleID,
                RoleName = user.RoleName,
                GenderID = user.GenderID,
                GenderName = user.GenderName,
                PhotoPath = user.PhotoPath,
                PhotoName = user.PhotoName,
                ActiveFlag = user.ActiveFlag,
                CreatedBy = user.CreatedBy,
                CreatedDate = user.CreatedDate,
                UpdatedBy = user.UpdatedBy,
                UpdatedDate = user.UpdatedDate
            };
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser([FromBody] UserCreation model)
        {
            var editModel = new UserCreationEditDTO();
            if (!(model.UserID > 0))
            {
                editModel = await ToEditDTO(model);
            }
           
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
                if(!(model.UserID > 0))
                {
                    json = null;
                    json = JsonConvert.SerializeObject(editModel);
                }
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
                    case "PasswordHash":
                        model.PasswordHash = field.Value;
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

        public IActionResult ValidatePasswordField([FromBody] Dictionary<string, string> fieldData)
        {

            var model = new UserCreationEditDTO(); // Assuming your model class is named 'User'

            // Bind only the incoming single field dynamically
            foreach (var field in fieldData)
            {
                switch (field.Key)
                {
                    case "UserName":
                        model.UserName = field.Value;
                        break;
                    case "PasswordHash":
                        model.PasswordHash = field.Value;
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
        public async Task<IActionResult> DeactivateUser([FromBody] int userId)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/ActivateDeactivateUser";

            var model = new UserCreationDTO();
            model.CreatedBy = user.UserID;
            model.UserID = userId;
            model.RecType = "DEACTIVATE";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            if (resp.IsSuccessStatusCode)
            {
                var responseString = await resp.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                int resUserId = result.userID;

                if (resUserId > 0)
                {
                    return Json(new { success = true, message = "success" });
                }
                
                else
                {
                    return Json(new { success = true, message = "error" });

                }
            }

            return Json(new { success = false, message = "Could not save user. Please try again." });
        
        }



        [HttpPost]
        public async Task<IActionResult> ActivateUser([FromBody] int userId)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserCreation + "/ActivateDeactivateUser";

            var model = new UserCreationDTO();
            model.CreatedBy = user.UserID;
            model.UserID = userId;
            model.RecType = "ACTIVATE";
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            if (resp.IsSuccessStatusCode)
            {
                var responseString = await resp.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                int resUserId = result.userID;

                if (resUserId > 0)
                {
                    return Json(new { success = true, message = "success" });
                }

                else
                {
                    return Json(new { success = true, message = "error" });

                }
            }

            return Json(new { success = false, message = "Could not save user. Please try again." });

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
            var user = TokenHelper.UserFromToken(HttpContext);
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
                List<ExtraPermission> extraPemission = await GetExtraPermissionByUserId(userId);
                var extraPermissionByMenu = extraPemission
                        .GroupBy(x => x.MenuModuleID)
                        .ToDictionary(g => g.Key, g => g.ToList());
                foreach (var module in parent.Value)
                {
                    var allChecked = module.Value.All(p => p.HasPermission);

                    sb.Append($@"
                <div class='card border-0 shadow-sm mb-3'>
                    <div class='card-header d-flex justify-content-between align-items-center bg-white'>
                        <strong class='text-secondary'><i class='bi bi-box me-2'></i>{module.Key}</strong>
                        <div class='form-check form-switch'>
                            <input class='form-check-input select-all' type='checkbox' data-module='{module.Key}' disabled {(allChecked ? "checked" : "")}>
                            <label class='form-check-label fw-semibold text-primary'>Select All</label>
                        </div>
                    </div>
                    <div class='card-body pt-3'>
                        <div class='row g-2'>");

                    foreach (var p in module.Value)
                    {
                        bool isCheckedFromExtra = extraPermissionByMenu.TryGetValue(p.MenuModuleID, out var extraList)
                                                  && extraList.Any(x => x.ActionID == p.ActionID);

                        // Final checked status: true if either HasPermission or extra permission
                        bool isChecked = p.HasPermission || isCheckedFromExtra;

                        // If it's checked from system (HasPermission), disable it.
                        // If it's checked from extra permission, do NOT disable.
                        string disabledAttr = (p.HasPermission && !isCheckedFromExtra) ? "disabled" : "";
                        bool isNotExtraPermision = p.HasPermission && !isCheckedFromExtra;
                        // Add 'extra' class only if unchecked
                        string checkboxClass = isNotExtraPermision
                            ? "form-check-input perm-checkbox"
                            : "form-check-input perm-checkbox extra";

                     

                        sb.Append($@"
                        <div class='col-md-2 col-sm-4 col-6'>
                            <div class='form-check form-switch'>
                                <input class='{checkboxClass}' type='checkbox'
                                       data-menuid='{p.MenuModuleID}' data-actionid='{p.ActionID}' data-module='{module.Key}'
                                       id='chk_{p.MenuModuleID}_{p.ActionID}' {(isChecked ? "checked='checked'" : "")} {disabledAttr}>
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


        [HttpPost]
        public async Task<IActionResult> SaveExtraPermissions( [FromBody] List<ExtraPermission> permissions)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(permissions), Encoding.UTF8, "application/json");
            var user = TokenHelper.UserFromToken(HttpContext);
            // assuming userId = 1 for demo
            var response = await client.PostAsync($"{_api.BaseUrlUserCreation}/SaveExtraPermission/{user.UserID}", content);
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


      
        public async Task<List<ExtraPermission>> GetExtraPermissionByUserId(int UserId)
        {
            var client = _httpClientFactory.CreateClient();
      
            var creater = TokenHelper.UserFromToken(HttpContext);
            // assuming userId = 1 for demo
            var response = await client.GetAsync($"{_api.BaseUrlUserCreation}/GetExtraPermissionByUserId/{UserId}/{creater.UserID}");
            if (!response.IsSuccessStatusCode)
            {
                var error = new List<ExtraPermission>();
                return error;
            }
            else
            {
                var json = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject < List<ExtraPermission> > (json);
                return res;
            }



        }


        [HttpPost]
        public async Task<IActionResult> SaveUserNotification([FromBody] UserNotification model)
        {
            try
            {
                if (model == null || model.UserID <= 0)
                    return Json(new { success = false, message = "Invalid user data." });

                // Default values
                model.IsActive = 1;
                model.IsRead = 0;
                model.CreatedDateTime = DateTime.Now;

                // Prepare HTTP Client
                var client = _httpClientFactory.CreateClient();
                var url = _api.BaseUrlUserNotification + "/SaveUserNotification";

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send API request
                var resp = await client.PostAsync(url, content);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"API error: {errorContent}" });
                }

                var response = await resp.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(response);

                return Json(new
                {
                    success = true,
                    message = "Notification saved successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Exception: {ex.Message}" });
            }
        }





    }
}
