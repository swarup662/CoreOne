using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using System.Net.Http.Headers;
using System.Text.Json;

public class NotificationHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSettingsHelper _apiSettings;

    private const string CacheKey = "__UserNotifications";

    public NotificationHelper(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        SettingsService settingsService)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _apiSettings = settingsService.ApiSettings;
    }

    /// <summary>
    /// Retrieves all notifications for the current user.
    /// Cached in HttpContext.Items for the request lifetime.
    /// </summary>
    public async Task<List<UserNotification>> GetUserNotificationsAsync(int userId)
    {
        var httpCtx = _httpContextAccessor.HttpContext;
        if (httpCtx == null)
            return new List<UserNotification>();

        // Check per-request cache first
        if (httpCtx.Items.ContainsKey(CacheKey))
            return httpCtx.Items[CacheKey] as List<UserNotification> ?? new List<UserNotification>();

        try
        {
            var token = httpCtx.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                httpCtx.Items[CacheKey] = new List<UserNotification>();
                return new List<UserNotification>();
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{_apiSettings.BaseUrlUserNotification}/GetUserNotifications";
            var content = new StringContent(JsonSerializer.Serialize(userId), System.Text.Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                httpCtx.Items[CacheKey] = new List<UserNotification>();
                return new List<UserNotification>();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var notifications = JsonSerializer.Deserialize<List<UserNotification>>(json, options)
                                ?? new List<UserNotification>();

            httpCtx.Items[CacheKey] = notifications;
            return notifications;
        }
        catch
        {
            httpCtx.Items[CacheKey] = new List<UserNotification>();
            return new List<UserNotification>();
        }
    }

    /// <summary>
    /// Marks a notification as read (API call).
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var httpCtx = _httpContextAccessor.HttpContext;
        if (httpCtx == null) return false;

        try
        {
            var token = httpCtx.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token))
                return false;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{_apiSettings.BaseUrlUserNotification}/MarkAsRead";
            var content = new StringContent(JsonSerializer.Serialize(notificationId), System.Text.Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url, content);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
