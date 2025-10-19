// File: CoreOne.API/Repositories/Interfaces/IPermissionRepository.cs
using CoreOne.DOMAIN.Models;
using System.Collections.Generic;
using System.Data;
namespace CoreOne.API.Interfaces 
{ 
public interface IPermissionRepository
{
    List<MenuItem> GetUserMenu(int userID);

    // NEW:
    List<ActionDto> GetUserAllowedActions(int userID);
    bool HasPermission(int userID, int menuModuleId, int actionId);
}
}
