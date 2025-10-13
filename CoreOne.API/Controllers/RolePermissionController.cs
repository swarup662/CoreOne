using CoreOne.API.Interfaces;
using CoreOne.COMMON.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CoreOne.API.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RolePermissionController : ControllerBase
    {
        private readonly IRolePermissionRepository _repository;

        public RolePermissionController(IRolePermissionRepository repository)
        {
            _repository = repository;
        }

        // Dropdown roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var data = await _repository.GetRolesAsync();
            return Ok(data);
        }

        // Grid roles
        [HttpPost("GetRoleGrid")]
        public IActionResult GetRoleGrid([FromBody] RoleGridRequest request)
        {
            if (request == null) return BadRequest("Request is required.");

            var dt = _repository.GetRoleGrid(
                request.PageSize,
                request.PageNumber,
                request.Search,
                request.SortColumn,
                request.SortDir
            );

            var roles = new List<RolePermissionModel>();
            foreach (DataRow row in dt.Rows)
            {
                roles.Add(new RolePermissionModel
                {
                    RoleID = Convert.ToInt32(row["RoleID"]),
                    RoleName = row["RoleName"]?.ToString() ?? string.Empty
                });
            }

            int totalRecords = 0;
            if (dt.Rows.Count > 0 && dt.Columns.Contains("TotalRecords"))
                totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

            var response = new RoleGridPagedResponse
            {
                Items = roles,
                TotalRecords = totalRecords,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortColumn = request.SortColumn,
                SortDir = request.SortDir,
                Search = request.Search
            };

            return Ok(response);
        }


        // Permissions by role
        [HttpGet("GetRolePermissionByRoleId/{roleId}")]
        public async Task<IActionResult> GetByRoleId(int roleId)
        {
            var data = await _repository.GetByRoleIdAsync(roleId);
            return Ok(data);
        }

        [HttpPost("SaveRolePermission/{roleId}/{userId}")]
        public async Task<IActionResult> Save(int roleId, int userId, [FromBody] IEnumerable<RolePermissionModel> permissions)
        {
            var result = await _repository.SaveAsync(permissions, roleId, userId);
            return Ok(result );
        }

        [HttpDelete("DeleteRolePermission/{roleID}/{userId}")]
        public async Task<IActionResult> Delete(int roleID, int userId)
        {
            var result = await _repository.DeleteAsync(roleID, userId);
            return Ok( result);
        }
    }
}
