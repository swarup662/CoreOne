using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.API.Repositories;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ModuleSetupController : ControllerBase
    {

        private readonly IModuleSetupRepository _menuModuleRepo;

        public ModuleSetupController(IRoleCreationRepository roleRepo, IModuleSetupRepository menuModuleRepo)
        {
            _menuModuleRepo = menuModuleRepo;
        }

        [HttpPost("GetMenuModule")]
        public IActionResult GetMenuModule([FromBody] RoleCreationsRequest request)
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
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedBy"]),
                    CreatedDate = row["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedDate"])
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

        [HttpGet("GetActionsDropdown")]
        public IActionResult GetActionsDropdown()
        {
            var dt = _menuModuleRepo.GetActionDropdown();

            var actions = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                actions.Add(new
                {
                    Id = row["ActionID"],
                    Name = row["ActionName"]
                });
            }

            return Ok(actions);
        }


        [HttpGet("GetModuleActionsByModuleID")]
        public IActionResult GetModuleActionsByModuleID(int moduleID)
        {
            var dt = _menuModuleRepo.GetActionsByModuleID(moduleID);
            var actions = new List<object>();

            foreach (DataRow row in dt.Rows)
            {
                actions.Add(new
                {
                    id = row["ActionID"],
                    name = row["ActionName"]
                });
            }

            return Ok(actions);
        }






        [HttpPost("SaveModuleActions")]
        public IActionResult SaveModuleActions([FromBody] SaveActionModel model)
        {
            if (model == null || model.Actions == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            try
            {
                _menuModuleRepo.SaveModuleActions(model.ModuleID, model.Actions, model.CreatedBy);
                return Ok(new { success = true, moduleId = model.ModuleID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }





    }
}
