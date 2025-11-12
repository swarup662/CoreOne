using CoreOne.API.Helpers;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;


        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            string userAgent = Request.Headers["User-Agent"].ToString();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // DO NOT deconstruct. Keep as single variable to avoid dynamic deconstruction errors.
            var loginResult = _authRepository.Login(request.UserName, request.Password, ipAddress, userAgent);

            if (!loginResult.Success)
                return Unauthorized(new { message = loginResult.Message });

            var user = loginResult.User;
            var token = loginResult.Token;
            var accessList = loginResult.AccessList ?? new List<UserAccessViewModel>();

            // External user → auto redirect
            if (user != null && !user.IsInternal)
            {
                // assume external has at least one entry
                var access = accessList.FirstOrDefault();
                if (access == null)
                    return BadRequest(new { message = "No application mapped for external user" });

                // call CreateCacheKeyAndGetRedirectUrl - do not deconstruct
                var cacheResult = _authRepository.CreateCacheKeyAndGetRedirectUrl(
                    user.UserID,
                    (int)access.CompanyID,
                    (int)access.ApplicationID,
                    (int)access.RoleID,
                    ipAddress);

                // cacheResult may be named tuple or plain tuple; use Item1/Item2/Item3 which always works
                bool ok = false;
                string msg = "Unknown error";
                string redirect = null;

                // if repository returned named tuple with properties Ok/Message/RedirectUrl:
                // use them, else fallback to Item1..Item3
                try
                {
                    // try property access first (works if named tuple or object with props)
                    var propOk = cacheResult.GetType().GetProperty("Ok");
                    if (propOk != null)
                    {
                        ok = (bool)propOk.GetValue(cacheResult);
                        msg = cacheResult.GetType().GetProperty("Message")?.GetValue(cacheResult)?.ToString();
                        redirect = cacheResult.GetType().GetProperty("RedirectUrl")?.GetValue(cacheResult)?.ToString();
                    }
                    else
                    {
                        // fallback to Item1/Item2/Item3
                        ok = (bool)cacheResult.Item1;
                        msg = cacheResult.Item2?.ToString();
                        redirect = cacheResult.Item3?.ToString();
                    }
                }
                catch
                {
                    // safest fallback - try ItemX directly
                    try
                    {
                        ok = (bool)cacheResult.Item1;
                        msg = cacheResult.Item2?.ToString();
                        redirect = cacheResult.Item3?.ToString();
                    }
                    catch
                    {
                        return StatusCode(500, new { message = "Cache result format invalid" });
                    }
                }

                if (!ok)
                    return BadRequest(new { message = msg ?? "Unable to create redirect" });

                return Ok(new { token, redirectUrl = redirect });
            }

            // Internal user → return token and access list
            return Ok(new { user,token, accessList });
        }

        [HttpPost("create-cachekey")]
        public IActionResult CreateCacheKey([FromBody] CreateCacheKeyRequest req)
        {

            string userAgent = Request.Headers["User-Agent"].ToString();
            string ip= HttpContext.Connection.RemoteIpAddress?.ToString();

            var cacheResult = _authRepository.CreateCacheKeyAndGetRedirectUrl(  req.UserID, req.CompanyID, req.ApplicationID, req.RoleID, ip,req.UrlType);

            // same robust extraction as above
            bool ok = false;
            string msg = "Unknown error";
            string redirect = null;

            try
            {
                var propOk = cacheResult.GetType().GetProperty("Ok");
                if (propOk != null)
                {
                    ok = (bool)propOk.GetValue(cacheResult);
                    msg = cacheResult.GetType().GetProperty("Message")?.GetValue(cacheResult)?.ToString();
                    redirect = cacheResult.GetType().GetProperty("RedirectUrl")?.GetValue(cacheResult)?.ToString();
                }
                else
                {
                    ok = (bool)cacheResult.Item1;
                    msg = cacheResult.Item2?.ToString();
                    redirect = cacheResult.Item3?.ToString();
                }
            }
            catch
            {
                try
                {
                    ok = (bool)cacheResult.Item1;
                    msg = cacheResult.Item2?.ToString();
                    redirect = cacheResult.Item3?.ToString();
                }
                catch
                {
                    return StatusCode(500, new { message = "Cache result format invalid" });
                }
            }

            if (!ok)
                return BadRequest(new { message = msg });

            return Ok(new { redirectUrl = redirect });
        }

        [HttpPost("exchange-cachekey")]
        public IActionResult ExchangeCacheKey([FromBody] ExchangeCacheKeyRequest req)
        {

            string userAgent = Request.Headers["User-Agent"].ToString();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var exchangeResult = _authRepository.ExchangeCacheKeyForToken(req.CacheKey, req.ApplicationID, ipAddress, userAgent);

            bool ok = false;
            string msg = "Unknown error";
            string token = null;

            try
            {
                var propOk = exchangeResult.GetType().GetProperty("Ok");
                if (propOk != null)
                {
                    ok = (bool)propOk.GetValue(exchangeResult);
                    msg = exchangeResult.GetType().GetProperty("Message")?.GetValue(exchangeResult)?.ToString();
                    token = exchangeResult.GetType().GetProperty("Token")?.GetValue(exchangeResult)?.ToString();
                }
                else
                {
                    ok = (bool)exchangeResult.Item1;
                    msg = exchangeResult.Item2?.ToString();
                    token = exchangeResult.Item3?.ToString();
                }
            }
            catch
            {
                try
                {
                    ok = (bool)exchangeResult.Item1;
                    msg = exchangeResult.Item2?.ToString();
                    token = exchangeResult.Item3?.ToString();
                }
                catch
                {
                    return StatusCode(500, new { message = "Exchange result format invalid" });
                }
            }

            if (!ok)
                return Unauthorized(new { message = msg });

            return Ok(new { access_token = token, token_type = "Bearer", expires_in = 900 });
        }

        // LOGOUT
        [HttpPost("Logout")]
        public IActionResult Logout([FromBody] int userID)
        {

            string userAgent = Request.Headers["User-Agent"].ToString();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            int result = _authRepository.Logout(userID, ipAddress, userAgent);
            return Ok(result > 0 ? "Logged out successfully" : "Logout failed");
        }

        [HttpPost("LogHttpError")]
        public IActionResult LogHttpError([FromBody] LogHttpErrorRequest request)
        {
            // Call repository to log error
            int result = _authRepository.LogHttpError(request);
            return Ok(result);
        }

        [HttpPost("LogException")]
        public IActionResult LogException([FromBody] LogExceptionRequest request)
        {
            // Call repository to log exception
            int result = _authRepository.LogException(request);
            return Ok(result);
        }






        #region change-forgot-password
        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword([FromBody] string email)
        {
            var result = _authRepository.ForgotPassword(email);
            return Ok(result);
        }

        [HttpGet("ValidateResetToken")]
        public IActionResult ValidateResetToken(string token)
        {
            var result = _authRepository.ValidateResetToken(token);
            return Ok(result);
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var result = _authRepository.ResetPassword(req.UserID, req.NewPassword, req.Token);
            return Ok(result);
        }

        [HttpPost("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var result = _authRepository.ChangePassword(req.UserID, req.CurrentPassword, req.NewPassword);
            return Ok(result);
        }


        #endregion

    }



    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
