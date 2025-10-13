using CoreOne.API.Repositories;
using CoreOne.COMMON.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CoreOne.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UserController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // CREATE / UPDATE USER
        [HttpPost("SaveUser")]
        public IActionResult SaveUser([FromBody] User model)
        {
            int result = _userRepo.SaveUser(model);
            return Ok(result > 0 ? "Saved successfully" : "Error saving user");
        }

        // ASSIGN EXTRA PERMISSIONS
        [HttpPost("AssignPermissions")]
        public IActionResult AssignPermissions([FromBody] List<UserPermissionModel> permissions)
        {
            if (permissions == null || permissions.Count == 0)
                return BadRequest("No permissions provided");

            int result = _userRepo.AssignPermissions(permissions);
            return Ok(result > 0 ? "Permissions assigned successfully" : "Failed to assign permissions");
        }

        // GET ALL USERS
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var dt = _userRepo.GetAllUsers();
            return Ok(dt);
        }
    }
}
