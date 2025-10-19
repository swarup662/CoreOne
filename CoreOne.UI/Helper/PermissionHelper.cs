using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using System.Net.Http.Headers;
using System.Text.Json;

public class PermissionHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSettingsHelper _apiSettings;

    private const string CacheKey = "__AllAllowedActions";

    public PermissionHelper(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, SettingsService settingsService)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _apiSettings = settingsService.ApiSettings;
    }

    /// <summary>
    /// Returns true if the current user has permission for the given ModuleId + ActionId
    /// </summary>
    public async Task<bool> HasPermissionAsync(int moduleId, int actionId)
    {
        var httpCtx = _httpContextAccessor.HttpContext;
        if (httpCtx == null) return false;

        // Cache permissions in HttpContext.Items for the lifetime of the request
        if (!httpCtx.Items.ContainsKey(CacheKey))
        {
            var token = httpCtx.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                httpCtx.Items[CacheKey] = new HashSet<(int, int)>();
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"{_apiSettings.BaseUrlPermission}/GetUserPermissions/";
                var res = await client.GetAsync(url);

                if (!res.IsSuccessStatusCode)
                {
                    httpCtx.Items[CacheKey] = new HashSet<(int, int)>();
                }
                else
                {
                    var json = await res.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var list = JsonSerializer.Deserialize<List<ActionDto>>(json, options)
                               ?? new List<ActionDto>();
                    var allowed = list.Select(a => (a.ModuleId, a.ActionID)).ToHashSet();
                    httpCtx.Items[CacheKey] = allowed;
                }
            }
            catch
            {
                httpCtx.Items[CacheKey] = new HashSet<(int, int)>();
            }
        }

        var allowedPairs = httpCtx.Items[CacheKey] as HashSet<(int, int)> ?? new HashSet<(int, int)>();
            bool bo = allowedPairs.Contains((moduleId, actionId));
        return allowedPairs.Contains((moduleId, actionId));
    }
}
