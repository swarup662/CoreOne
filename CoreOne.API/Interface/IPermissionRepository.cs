// File: CoreOne.API/Repositories/Interfaces/IPermissionRepository.cs
using CoreOne.COMMON.Models;
using System.Collections.Generic;
using System.Data;
namespace CoreOne.API.Interfaces 
{ 
public interface IPermissionRepository
{
    List<MenuItem> GetUserMenu(int userID);
    DataTable GetModules();
    DataTable GetActions();
    int AssignRolePermission(RolePermission model);

    // NEW:
    List<ActionDto> GetUserAllowedActions(int userID);
    bool HasPermission(int userID, int menuModuleId, int actionId);
}
}
