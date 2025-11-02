using CoreOne.DOMAIN.Models;
using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace CoreOne.UI.Middleware
{
    public class UiErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ApiSettingsHelper _api;
        public UiErrorLoggingMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration config, SettingsService settingsService)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _api = settingsService.ApiSettings;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                // Log non-success HTTP status codes (optional)
                if (context.Response.StatusCode != 200)
                {
                    await LogHttpError(context);
                    // Redirect to Error page
                

                }
            }
            catch (Exception ex)
            {
                await LogException(ex, context);

                
            }
        }

        private async Task LogHttpError(HttpContext context)
        {
            try
            {
                 var userId = TokenHelper.GetUserIdFromToken(context);
                if(userId == null)
                {
                    userId = 0;
                }
                LogHttpErrorRequest payload = new LogHttpErrorRequest
                {
                    UserID = userId.ToString(),
                    StatusCode = context.Response.StatusCode,
                    RequestPath = context.Request.Path.ToString(),
                    Headers = JsonConvert.SerializeObject(context.Request.Headers)
                };
            
                var client = _httpClientFactory.CreateClient();
               
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response= await client.PostAsync(_api.BaseUrlAuth + "/LogHttpError", content);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        private async Task LogException(Exception ex, HttpContext context)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken(context);
                if (userId == null)
                {
                    userId = 0;
                }
                LogExceptionRequest payload = new LogExceptionRequest
                {
                    UserID = userId.ToString(),
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = context.Request.Path.ToString(),
                    Headers = JsonConvert.SerializeObject(context.Request.Headers)
                };
             
                var client = _httpClientFactory.CreateClient();
               
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response =await client.PostAsync(_api.BaseUrlAuth + "/LogException", content);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
