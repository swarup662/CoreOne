using CoreOne.COMMON.Models;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IRolePermissionRepository
    {
        Task<IEnumerable<RolePermissionModel>> GetRolesAsync(); // Dropdown (Roles)
        DataTable GetRoleGrid(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir);
        Task<IEnumerable<RolePermissionModel>> GetByRoleIdAsync(int roleId); // Menu + Actions + Checked

        Task<int> SaveAsync(IEnumerable<RolePermissionModel> permissions, int roleId, int userId);
        Task<int> DeleteAsync(int roleID, int userId);
    }
}
