using CoreOne.COMMON.Models;
using System.Data;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IRoleRepository
    {
        DataTable GetRoles(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol);
        int GetTotalRoles(string? search, string? searchCol);
        int SaveRole(string recType, int? roleId, string roleName, string roleDescription, int userId);
        Role? GetRoleById(int roleId);
    }
}