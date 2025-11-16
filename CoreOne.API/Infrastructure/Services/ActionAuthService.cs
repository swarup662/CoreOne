using CoreOne.DOMAIN.Models;
using CoreOne.API.Helper;
using System.Net.Http.Headers;
using System.Text.Json;

public class ActionAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PermissionController _permissionController;


    private const string CacheKey = "__AllAllowedActions";

    public ActionAuthService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, PermissionController permissionController)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _permissionController = permissionController;
    }

    /// <summary>
    /// Returns true if the current user has permission for the given ModuleId + ActionId
    /// </summary>
    public async Task<bool> HasPermissionAsync(int UserID, int CurrentApplicationID, int CurrentCompanyID, int CurrentRoleID,int moduleId, int actionId)
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


                List<ActionDto> res = (List<ActionDto>)_permissionController.GetUserPermissions(UserID, CurrentApplicationID, CurrentCompanyID, CurrentRoleID);

                if (res==null)
                {
                    httpCtx.Items[CacheKey] = new HashSet<(int, int)>();
                }
                else
                {
                    List<ActionDto> list = res;

                    //var options = new JsonSerializerOptions
                    //{
                    //    PropertyNameCaseInsensitive = true
                    //};

                    //var list = JsonSerializer.Deserialize<List<ActionDto>>(json, options)
                    //           ?? new List<ActionDto>();
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
