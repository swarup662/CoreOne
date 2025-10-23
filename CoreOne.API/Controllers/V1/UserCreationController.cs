using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserCreationController : ControllerBase
    {
        private readonly IUserCreationRepository _userRepo;

        public UserCreationController(IUserCreationRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpPost("GetUsers")]
        public IActionResult GetUsers([FromBody] UserCreationRequest request)
        {
            if (request == null) return BadRequest("Request is required.");

            var dt = _userRepo.GetUsers(
                request.PageSize,
                request.PageNumber,
                request.Search,
                request.SortColumn,
                request.SortDir,
                request.SearchCol,
                request.Status
            );

            var users = new List<UserCreation>();
            foreach (DataRow row in dt.Rows)
            {
                users.Add(new UserCreation
                {
                    UserID = Convert.ToInt32(row["UserID"]),
                    UserName = row["UserName"]?.ToString(),
                    Email = row["Email"]?.ToString(),
                    PhoneNumber = row["PhoneNumber"]?.ToString(),
                    RoleID = row["RoleID"] == DBNull.Value ? null : Convert.ToInt32(row["RoleID"]),
                    RoleName = row["RoleName"]?.ToString(),  // ✅ Added RoleName
                    GenderID = row["GenderID"] == DBNull.Value ? null : Convert.ToInt32(row["GenderID"]),
                    GenderName = row["GenderName"]?.ToString(),  // ✅ Added RoleName
                    MailTypeID = row["MailTypeID"] == DBNull.Value ? null : Convert.ToInt32(row["MailTypeID"]),
                    PhotoPath = row["PhotoPath"]?.ToString(),
                    PhotoName = row["PhotoName"]?.ToString(),
                    CreatedDate = row["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedDate"]),  // ✅ Already present
                    UpdatedDate = row["UpdatedDate"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedDate"]),
                    ActiveFlag = row["ActiveFlag"] == DBNull.Value ? null : Convert.ToInt32(row["ActiveFlag"]),
                });
            }

            int totalRecords = 0;
            if (dt.Rows.Count > 0 && dt.Columns.Contains("TotalRecords"))
                totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

            var response = new
            {
                Items = users,
                TotalRecords = totalRecords,
                request.PageNumber,
                request.PageSize,
                request.SortColumn,
                request.SortDir,
                request.Search,
                request.SearchCol
            };

            return Ok(response);
        }

        [HttpPost("GetRoles")]
        public IActionResult GetRoles([FromBody] int userId)
        {
            if (userId <= 0) return BadRequest("Invalid UserID.");

            var user = _userRepo.GetRoles(userId);

            if (user == null)
                return NotFound(new { Message = "Roles not found." });

            return Ok(JsonConvert.SerializeObject(user));
        }

        [HttpPost("GetGenders")]
        public IActionResult GetGenders([FromBody] int userId)
        {
            if (userId <= 0) return BadRequest("Invalid UserID.");

            var user = _userRepo.GetGenders(userId);

            if (user == null)
                return NotFound(new { Message = "Roles not found." });


            return Ok(JsonConvert.SerializeObject(user));
        }

        [HttpPost("GetMailtypes")]
        public IActionResult GetMailtypes([FromBody] int userId)
        {
            if (userId <= 0) return BadRequest("Invalid UserID.");

            var user = _userRepo.GetMailtypes(userId);

            if (user == null)
                return NotFound(new { Message = "Roles not found." });


            return Ok(JsonConvert.SerializeObject(user));
        }

        [HttpPost("AddUser")]
        public IActionResult AddUser([FromBody] UserCreation user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserName))
                return BadRequest("Username is required.");

            int newId = _userRepo.SaveUser("INSERT", user);
            return Ok(new { UserID = newId, Message = "User created successfully." });
        }

        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser([FromBody] UserCreationEditDTO user)
        {
            if (user == null || user.UserID <= 0)
                return BadRequest("Valid user data required.");

            int result = _userRepo.UpdateUser("UPDATE", user);

            if (result > 0)
                return Ok(new { UserID = result, Message = "User updated successfully." });

            return NotFound(new { UserID = result, Message = "User not found or not updated." });
        }

        [HttpPost("ActivateDeactivateUser")]
        public IActionResult ActivateDeactivateUser( UserCreationDTO user)
        {
            //if (user.UserID <= 0) return BadRequest("Invalid UserID.");

            int result = _userRepo.ActivateDeactivateUser(user.RecType, user);

            if (result > 0)
                return Ok(new { UserID = result, Message = "User deleted successfully." });

            return NotFound(new { UserID = result, Message = "User not found or already deleted." });
        }

        [HttpPost("GetUserById")]
        public IActionResult GetUserById([FromBody] int userId)
        {
            if (userId <= 0) return BadRequest("Invalid UserID.");

            var user = _userRepo.GetUserById(userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(user);
        }
        [HttpPost("SaveExtraPermission/{UserId}")]
        public async Task<IActionResult> SaveExtraPermissionAsync(int UserId, [FromBody] IEnumerable<ExtraPermission> permissions)
        {
            var result = await _userRepo.SaveExtraPermissionAsync(UserId, permissions);
            return Ok(result);
        }

        [HttpGet("GetExtraPermissionByUserId/{UserId}/{CreatedBy}")]
        public async Task<IActionResult> GetByRoleId(int UserId, int CreatedBy)
        {
            var data = await _userRepo.GetExtraPermissionByUserId(UserId, CreatedBy);
            return Ok(data);
        }



    }
}
