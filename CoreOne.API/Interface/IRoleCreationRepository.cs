using CoreOne.DOMAIN.Models;
using System.Data;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IRoleCreationRepository
    {
        DataTable GetRoles(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol);
        int GetTotalRoles(string? search, string? searchCol);
        int SaveRole(string recType, int? roleId, string roleName, string roleDescription, int userId);
        RoleCreation? GetRoleById(int roleId);
    }
}