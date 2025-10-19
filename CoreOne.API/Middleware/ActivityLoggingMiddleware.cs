using CoreOne.API.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace CoreOne.API.Middleware
{
    public class ActivityLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DBContext _dbHelper;

        public ActivityLoggingMiddleware(RequestDelegate next, DBContext dbHelper)
        {
            _next = next;
            _dbHelper = dbHelper;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            try
            {
                var userIdClaim = context.User?.FindFirst("UserID")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    int userID = int.Parse(userIdClaim);
                    var parameters = new Dictionary<string, object>
                    {
                        { "@UserID", userID },
                        { "@ActivityDescription", $"Accessed {context.Request.Path} | Module: {context.Request.Headers["X-Module"]} | Action: {context.Request.Headers["X-Action"]}" },
                        { "@IPAddress", context.Connection.RemoteIpAddress?.ToString() }
                    };
                    _dbHelper.ExecuteSP_ReturnInt("sp_LogActivity", parameters);
                }
            }
            catch { }
        }
    }
}
