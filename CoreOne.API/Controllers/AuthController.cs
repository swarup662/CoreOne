using CoreOne.API.Helpers;
using CoreOne.API.Interfaces;
using CoreOne.COMMON.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CoreOne.API.Controllers
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


            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = _authRepository.Login(request.UserName, request.Password, ipAddress);

            if (!result.Success)
                return Unauthorized(new { Message = result.Message });

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

    }

    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
