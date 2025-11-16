// File: CoreOne.API/Controllers/PermissionController.cs
using CoreOne.API.Interfaces;
using CoreOne.API.Repositories;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PermissionController : ControllerBase
{
    private readonly IPermissionRepository _permissionRepo;

    public PermissionController(IPermissionRepository permissionRepo)
    {
        _permissionRepo = permissionRepo;
    }

 

    // KEEP: GetUserMenu (unchanged)
    [HttpGet("GetUserMenu/{userID}/{CurrentApplicationID}/{CurrentCompanyID}/{CurrentRoleID}")]
    public IActionResult GetUserMenu(int userID , int CurrentApplicationID, int CurrentCompanyID, int CurrentRoleID)
    {
        var menuList = _permissionRepo.GetUserMenu(userID, CurrentApplicationID , CurrentCompanyID, CurrentRoleID);
        return Ok(menuList);
    }

    // NEW: Allowed actions for current caller for a module
    [HttpGet("GetUserPermissions/{UserID}/{CurrentApplicationID}/{CurrentCompanyID}/{CurrentRoleID}")]
    public IActionResult GetUserPermissions(int UserID, int CurrentApplicationID, int CurrentCompanyID, int CurrentRoleID)
    {
        
        var list = _permissionRepo.GetUserAllowedActions(UserID, CurrentApplicationID, CurrentCompanyID, CurrentRoleID);
        return Ok(list);
    }

    // NEW: Check single action (POST)
    [HttpPost("HasPermission")]
    public IActionResult HasPermission([FromBody] HasPermissionRequest req)
    {
        var allowed = _permissionRepo.HasPermission(req,req.MenuModuleID, req.ActionID);
        return Ok(new { allowed });
    }

   
}
