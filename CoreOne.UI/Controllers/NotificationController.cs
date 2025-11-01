﻿using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text;

namespace CoreOne.UI.Controllers
{
    public class NotificationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettingsHelper _api;

        public NotificationController(IHttpClientFactory httpClientFactory, SettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
        }



        [HttpGet]
        public async Task<IActionResult> UserNotification(
                   int pageNumber = 1,
                   int pageSize = 10,
                   string? search = "",
                   string? searchCol = "Message",
                   string? sortColumn = "CreatedDateTime",
                   string? sortDir = "DESC",
                   int? createdBy = null)
        {
            var model = new UserNotificationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                SearchCol = searchCol,
                SortColumn = sortColumn,
                SortDir = sortDir,
                CreatedBy = createdBy
            };

            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserNotification + "/GetUserNotificationGrid";

            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);

            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Notifications = new List<UserNotificationDto>();
                ViewBag.TotalRecords = 0;
            }
            else
            {
                var response = await resp.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<UserNotificationPagedResponse>(response);
                ViewBag.NotificationList = await GetNotficationDropdown();
                ViewBag.Notifications = apiResult?.Items ?? new List<UserNotificationDto>();
                ViewBag.TotalRecords = apiResult?.TotalRecords ?? 0;
            }

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.SearchCol = searchCol;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDir = sortDir;

            return View();
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


        [HttpGet]
        public async Task<IActionResult> GetUserNotificationById(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_api.BaseUrlUserNotification}/GetUserNotificationById?id={id}";

                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Failed to fetch notification from API" });

                var responseContent = await resp.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<UserNotificationEdit>(responseContent);

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // ✅ Get user-specific notifications
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_api.BaseUrlUserNotification}/GetUserNotifications"; // Example endpoint

                var json = JsonConvert.SerializeObject(userId);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(url, content);

                if (!resp.IsSuccessStatusCode)
                    return Json(new List<UserNotification>());

                var response = await resp.Content.ReadAsStringAsync();
                var notifications = JsonConvert.DeserializeObject<List<UserNotification>>(response);

                return Json(notifications ?? new List<UserNotification>());
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }

        // ✅ Mark a notification as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] int notificationId)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_api.BaseUrlUserNotification}/MarkAsRead";

            var json = JsonConvert.SerializeObject(notificationId);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);

            return Json(resp.IsSuccessStatusCode ? new { success = true } : new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserNotification([FromBody] int NotificationID)
        {
            var user = TokenHelper.UserFromToken(HttpContext);
            var client = _httpClientFactory.CreateClient();
            var url = _api.BaseUrlUserNotification + "/DeleteUserNotification";

            var payload = new DeleteUserNotificationRequest
            {
                NotificationID = NotificationID,
                CreatedBy = user.UserID
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send request to API
            var resp = await client.PostAsync(url, content);
            var responseContent = await resp.Content.ReadAsStringAsync();

            // Try to deserialize API response (which contains success/message)
            dynamic? apiResponse = null;
            try
            {
                apiResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            }
            catch
            {
                // fallback if API doesn't return valid JSON
            }

            if (resp.IsSuccessStatusCode)
            {
                return Json(new
                {
                    success = apiResponse?.success ?? true,
                    message = apiResponse?.message ?? "Notification deleted successfully."
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = apiResponse?.message ?? "Failed to delete notification."
                });
            }
        }




    }
}
