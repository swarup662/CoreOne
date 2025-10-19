using System.Data;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;

namespace CoreOne.API.Repositories
{
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly DBContext _dbHelper;

        public RolePermissionRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // 1. Roles dropdown
        public async Task<IEnumerable<RolePermission>> GetRolesAsync()
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetRolesForDropdown"); // SELECT * FROM Roles
            var list = new List<RolePermission>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RolePermission
                {
                    RoleID = Convert.ToInt32(row["RoleID"]),
                    RoleName = row["RoleName"].ToString()
                });
            }

            return await Task.FromResult(list);
        }

        // 2. Roles for grid
        public DataTable GetRoleGrid(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir)
        {
            // Ensure defaults
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;
            sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? null : sortColumn;
            sortDir = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir;

            var parameters = new Dictionary<string, object>
    {
        {"@PageSize", pageSize},
        {"@PageNumber", pageNumber},
        {"@Search", (object?)search ?? DBNull.Value},
        {"@SortColumn", sortColumn},
        {"@SortDir", sortDir }
    };

            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetRoleGrid", parameters);
        }


        // 3. Permissions by role id
        public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId)
        {
            var parameters = new Dictionary<string, object> { { "@RoleID", roleId } };
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetRolePermissionsByRoleID", parameters);

            var list = new List<RolePermission>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RolePermission
                {
                    RoleID = roleId,
                    MenuModuleID = Convert.ToInt32(row["MenuModuleID"]),
                    ModuleName = row["ModuleName"].ToString(),
                    ParentMenuID = row["ParentMenuID"] != DBNull.Value ? Convert.ToInt32(row["ParentMenuID"]) : null,
                    ParentMenuName = row["ParentMenuName"]?.ToString(),
                    ActionID = Convert.ToInt32(row["ActionID"]),
                    ActionName = row["ActionName"].ToString(),
                    HasPermission = Convert.ToBoolean(row["HasPermission"])
                });
            }

            return await Task.FromResult(list);
        }

        public async Task<int> SaveAsync(IEnumerable<RolePermission> rolePermissions, int roleId, int userId)
        {
            var dt = new DataTable();
            dt.Columns.Add("RoleID", typeof(int));
            dt.Columns.Add("MenuModuleID", typeof(int));
            dt.Columns.Add("ActionID", typeof(int));
            dt.Columns.Add("ActiveFlag", typeof(int));
        

            foreach (var rp in rolePermissions)
            {
                int ActiveFlag = 0;
                if (rp.HasPermission)
                {
                    ActiveFlag = 1;
                }
                else
                {
                    ActiveFlag = 0;
                }
                dt.Rows.Add(
                    
                    rp.RoleID,
                    rp.MenuModuleID,
                    rp.ActionID,
                    ActiveFlag

                );
            }

            var parameters = new Dictionary<string, object>
            {
                { "@RoleId", roleId },
                { "@UserID", userId }
            };
 
            var result = _dbHelper.ExecuteSP_WithTableType_ReturnInt("sp_SaveRolePermissions", "Permissions", "RolePermissionTableType", dt, parameters);
            return await Task.FromResult(result);
        }

        public async Task<int> DeleteAsync(int roleID, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@RoleID", roleID },
                { "@UserID", userId }
            };
            return await Task.FromResult(_dbHelper.ExecuteSP_ReturnInt("sp_DeleteRolePermission", parameters));
        }
    }
}
