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
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationRepository _authRepository;


        public AuthenticationController(IAuthenticationRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {


            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = _authRepository.Login(request.UserName, request.Password, ipAddress);

            if (!result.Success)
                return Unauthorized(new { result.Message });

            return Ok(new
            {
                token = result.Token,
                userID = result.User.UserID,
                userName = result.User.UserName,
                roleID = result.User.RoleID,

            });
        }



        // LOGOUT
        [HttpPost("Logout")]
        public IActionResult Logout([FromBody] int userID)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            int result = _authRepository.Logout(userID, ip);
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






        #region chage-forgot-password
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
