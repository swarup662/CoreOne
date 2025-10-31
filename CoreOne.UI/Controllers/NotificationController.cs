using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
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
    }
}
