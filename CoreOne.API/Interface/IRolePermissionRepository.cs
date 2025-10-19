using CoreOne.DOMAIN.Models;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IRolePermissionRepository
    {
        Task<IEnumerable<RolePermission>> GetRolesAsync(); // Dropdown (Roles)
        DataTable GetRoleGrid(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir);
        Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId); // Menu + Actions + Checked

        Task<int> SaveAsync(IEnumerable<RolePermission> permissions, int roleId, int userId);
        Task<int> DeleteAsync(int roleID, int userId);
    }
}
