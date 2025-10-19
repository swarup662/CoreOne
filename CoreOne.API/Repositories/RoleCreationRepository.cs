using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class RoleCreationRepository : IRoleCreationRepository
    {
        private readonly DBContext _dbHelper;

        public RoleCreationRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetRoles(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol)
        {
            // Ensure defaults
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;
            sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? null : sortColumn;
            sortDir = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir;
            searchCol = string.IsNullOrWhiteSpace(searchCol) ?  null : searchCol;

            var parameters = new Dictionary<string, object>
            {
                {"@PageSize", pageSize},
                {"@PageNumber", pageNumber},
                {"@Search", (object?)search ?? DBNull.Value},
                {"@SearchCol", searchCol},
                {"@SortColumn", sortColumn},
                {"@SortDir", sortDir }
            };

            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetRoles", parameters);
        }

        public int GetTotalRoles(string? search, string? searchCol)
        {
            var dt = GetRoles(1, 1, search, "RoleName", "ASC", searchCol ?? "RoleName");
            if (dt.Rows.Count > 0 && dt.Columns.Contains("TotalRecords"))
                return Convert.ToInt32(dt.Rows[0]["TotalRecords"]);
            return 0;
        }

        public int SaveRole(string recType, int? roleId, string roleName, string roleDescription, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@RecType", recType },          // "INSERT" | "UPDATE" | "DELETE"
                { "@RoleID", roleId },            // NULL for insert
                { "@RoleName", roleName },        // required for insert/update
                { "@RoleDescription", roleDescription },
                { "@UserID", userId }
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_Roles_CRUD", parameters);
        }



        public RoleCreation? GetRoleById(int roleId)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_Roles_CRUD", new Dictionary<string, object>
            {
                { "@RecType", "GETBYID" },
                { "@RoleID", roleId }
            });

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new RoleCreation
            {
                RoleID = Convert.ToInt32(row["RoleID"]),
                RoleName = row["RoleName"]?.ToString() ?? string.Empty,
                RoleDescription = row["RoleDescription"]?.ToString() ?? string.Empty,
                CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                CreatedDate = row["CreatedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["CreatedDate"]),
                 ActiveFlag = row["ActiveFlag"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["ActiveFlag"]),
            };
        }

    }
}
