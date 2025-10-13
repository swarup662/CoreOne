using CoreOne.API.Helpers;
using CoreOne.API.Interfaces;
using CoreOne.COMMON.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoreOne.API.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly DBHelper _dbHelper;

        public PermissionRepository(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<MenuItem> GetUserMenu(int userID)
        {
            var parameters = new Dictionary<string, object> { { "@UserID", userID } };
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserMenu", parameters);

            return dt.AsEnumerable()
                     .Select(row => new MenuItem
                     {
                         MenuID = row.Field<int>("MenuID"),
                         MenuName = row.Field<string>("MenuName"),
                         Url = row.Field<string>("Url"),
                         MenuSequence = row.Field<int>("MenuSequence"),
                         ParentID = row.Field<int?>("ParentID"),
                         ParentName = row.Field<string>("ParentName"),
                         ParentSequence = row.Field<int?>("ParentSequence"),
                         MenuSymbol = row.Field<string>("MenuSymbol"),
                     })
                     .OrderBy(m => m.ParentSequence)
                     .ThenBy(m => m.MenuSequence)
                     .ToList();
        }

        public DataTable GetModules()
        {
            var ds = _dbHelper.ExecuteSP_ReturnDataSet("sp_GetAllModules", new Dictionary<string, object>());
            return ds.Tables[0];
        }

        public DataTable GetActions()
        {
            var ds = _dbHelper.ExecuteSP_ReturnDataSet("sp_GetAllActions", new Dictionary<string, object>());
            return ds.Tables[0];
        }

        public int AssignRolePermission(RolePermission model)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@RoleID", model.RoleID },
                {"@MenuModuleID", model.ModuleID },
                {"@ActionID", model.ActionID },
                {"@ActiveFlag", 1 },
                {"@CreatedBy", model.CreatedBy }
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_SaveRolePermission", parameters);
        }

        public List<ActionDto> GetUserAllowedActions(int userID)
        {
            var parameters = new Dictionary<string, object>
        {
            { "@UserID", userID },
            { "@MenuModuleID", 0 }
        };

            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserAllowedActions", parameters);

            var list = new List<ActionDto>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ActionDto
                {
                    ActionID = row.Field<int>("ActionID"),
                    ModuleId = row.Field<int>("ModuleId"),
                    ActionName = row.Field<string>("ActionName")
                });
            }
            return list;
        }

        public bool HasPermission(int userID, int menuModuleId, int actionId)
        {
            var parameters = new Dictionary<string, object>
        {
            { "@UserID", userID },
            { "@MenuModuleID", menuModuleId },
            { "@ActionID", actionId }
        };

            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_CheckUserPermission", parameters);
            if (dt.Rows.Count == 0) return false;

            // Your SP returns 1 or 0 in first column
            object val = dt.Rows[0][0];
            if (val == DBNull.Value) return false;
            return Convert.ToInt32(val) == 1;
        }
    }
}
