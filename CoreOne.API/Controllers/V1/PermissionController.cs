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

    private int? GetUserIdFromBearer()
    {
        var auth = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ")) return null;
        var token = auth.Substring("Bearer ".Length).Trim();
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var jwt = handler.ReadJwtToken(token);
            var claim = jwt.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            return int.TryParse(claim, out var uid) ? uid : (int?)null;
        }
        catch { return null; }
    }

    // KEEP: GetUserMenu (unchanged)
    [HttpGet("GetUserMenu/{userID}")]
    public IActionResult GetUserMenu(int userID)
    {
        var menuList = _permissionRepo.GetUserMenu(userID);
        return Ok(menuList);
    }

    // NEW: Allowed actions for current caller for a module
    [HttpGet("GetUserPermissions")]
    public IActionResult GetUserPermissions()
    {
        var caller = GetUserIdFromBearer();
        if (caller == null) return Unauthorized();
        var list = _permissionRepo.GetUserAllowedActions(caller.Value);
        return Ok(list);
    }

    // NEW: Check single action (POST)
    [HttpPost("HasPermission")]
    public IActionResult HasPermission([FromBody] HasPermissionRequest req)
    {
        var caller = GetUserIdFromBearer();
        if (caller == null) return Unauthorized();

        var allowed = _permissionRepo.HasPermission(caller.Value, req.MenuModuleID, req.ActionID);
        return Ok(new { allowed });
    }

   
}
