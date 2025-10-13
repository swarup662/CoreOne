using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.COMMON.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MenuModuleController : ControllerBase
    {
       
        private readonly IMenuModuleRepository _menuModuleRepo;

        public MenuModuleController(IRoleRepository roleRepo, IMenuModuleRepository menuModuleRepo)
        {
            _menuModuleRepo = menuModuleRepo;
        }

        [HttpPost("GetMenuModule")]
        public IActionResult GetMenuModule([FromBody] RolesRequest request)
        {
            if (request == null) return BadRequest("Request is required.");

            // Call repository
            var dt = _menuModuleRepo.GetMenuModule(
                request.PageSize,
                request.PageNumber,
                request.Search,
                request.SortColumn,
                request.SortDir,
                request.SearchCol // kept for interface compatibility
            );

            // Map DataTable to MenuModuleDto
            var modules = new List<MenuModuleDto>();
            foreach (DataRow row in dt.Rows)
            {
                modules.Add(new MenuModuleDto
                {
                    MenuModuleID = Convert.ToInt32(row["MenuModuleID"]),
                    MenuName = row["MenuName"]?.ToString() ?? string.Empty,
                    MenuSymbol = row["MenuSymbol"]?.ToString() ?? string.Empty,
                    Modules = row["Modules"]?.ToString() ?? string.Empty, // comma-separated submenus
                    Sequence = row["Sequence"] == DBNull.Value ? 0 : Convert.ToInt32(row["Sequence"]),
                    ActiveFlag = row["ActiveFlag"] == DBNull.Value ? 0 : Convert.ToInt32(row["ActiveFlag"]),
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                    CreatedDate = row["CreatedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["CreatedDate"])
                });
            }

            // Get total records for pagination
            int totalRecords = 0;
            if (dt.Rows.Count > 0 && dt.Columns.Contains("TotalRecords"))
                totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

            // Prepare paged response
            var response = new MenuModulePagedResponse
            {
                Items = modules,
                TotalRecords = totalRecords,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortColumn = request.SortColumn,
                SortDir = request.SortDir,
                Search = request.Search,
                SearchCol = request.SearchCol
            };

            return Ok(response);
        }

        [HttpPost("SaveMenuWithModules")]
        public IActionResult SaveMenuWithModules([FromBody] MenuWithModulesSave model)
        {
            try
            {
                int menuId = _menuModuleRepo.SaveMenuWithModules(model);
                if (menuId < 0)
                    return BadRequest(new { message = "Menu name already exists." });

                return Ok(new { menuId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("GetMenuModuleById/{id}")]
        public IActionResult GetMenuModuleById(int id)
        {
            if (id <= 0) return BadRequest("Invalid MenuModuleID");

            try
            {
                var menu = _menuModuleRepo.GetMenuModuleById(id);
                if (menu == null) return NotFound();

                return Ok(menu); // menu object contains Menu + Modules
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




    }
}
