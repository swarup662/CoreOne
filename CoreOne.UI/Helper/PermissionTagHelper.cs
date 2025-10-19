// File: CoreOne.UI/TagHelpers/PermissionTagHelper.cs
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using CoreOne.DOMAIN.Models; // ensure ActionDto available or create local DTO
using System.Linq;
using System.Text;
using CoreOne.UI.Helper;

[HtmlTargetElement("has-permission")]
public class PermissionTagHelper : TagHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSettingsHelper _apiSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionTagHelper(IHttpClientFactory httpClientFactory, SettingsService settingsService, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _apiSettings = settingsService.ApiSettings;
        _httpContextAccessor = httpContextAccessor;
    }

    [HtmlAttributeName("module-id")]
    public int ModuleId { get; set; }

    [HtmlAttributeName("action-id")]
    public int ActionId { get; set; }

    // Allows access to current ViewContext and HttpContext
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var httpCtx = ViewContext.HttpContext;
        const string key = "__AllAllowedActions";

        if (!httpCtx.Items.ContainsKey(key))
        {
            var token = httpCtx.Request.Cookies["jwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                output.SuppressOutput();
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Fetch ALL allowed actions for user in one call
                var url = $"{_apiSettings.BaseUrlPermission}/GetUserPermissions/";
                var res = await client.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    httpCtx.Items[key] = new HashSet<(int, int)>();
                }
                else
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<ActionDto>>(json)
           ?? new List<ActionDto>();
                    var allowed = list.Select(a => (a.ModuleId, a.ActionID)).ToHashSet();
                    httpCtx.Items[key] = allowed;
                }
            }
            catch
            {
                httpCtx.Items[key] = new HashSet<(int, int)>();
            }
        }

        var allowedPairs = httpCtx.Items[key] as HashSet<(int, int)> ?? new();
        if (!allowedPairs.Contains((ModuleId, ActionId)))
        {
            output.SuppressOutput();
            return;
        }

        var child = await output.GetChildContentAsync();
        output.Content.SetHtmlContent(child);
    }

}
