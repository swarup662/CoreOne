using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoreOne.API.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly DBContext _dbHelper;

        public PermissionRepository(DBContext dbHelper)
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
