using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class ModuleSetupRepository : IModuleSetupRepository
    {
        private readonly DBContext _dbHelper;

        public ModuleSetupRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }
        public DataTable GetMenuModule(
        int pageSize,
        int pageNumber,
        string? search,
        string? sortColumn,
        string? sortDir,
        string? searchCol // now used
    )
        {
            // Ensure default values
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;
            sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "MenuName" : sortColumn;
            sortDir = string.IsNullOrWhiteSpace(sortDir) ? "ASC" : sortDir;

            // Prepare parameters for SP
            var parameters = new Dictionary<string, object>
    {
        {"@PageSize", pageSize },
        {"@PageNumber", pageNumber },
        {"@Search", (object?)search ?? DBNull.Value },
        {"@SortColumn", sortColumn },
        {"@SortDir", sortDir },
        {"@SearchCol", (object?)searchCol ?? DBNull.Value } // pass searchCol to SP
    };

            // Execute stored procedure and return DataTable
            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetMenuModuleGrid", parameters);
        }


        // Save Menu (Insert/Update)
        public int SaveMenuWithModules(MenuWithModulesSave model)
        {
            // Prepare modules TVP
            var dtModules = new DataTable();
            dtModules.Columns.Add("ModuleID", typeof(int));
            dtModules.Columns.Add("Name", typeof(string));
            dtModules.Columns.Add("Url", typeof(string));
            dtModules.Columns.Add("Sequence", typeof(int));

            foreach (var m in model.Modules)
            {
                dtModules.Rows.Add(
                    m.ModuleID ?? (object)DBNull.Value,
                    m.Name,
                    m.Url,
                    m.Sequence
                );
            }

            // Scalar parameters
            var parameters = new Dictionary<string, object>
        {
            { "@MenuModuleID", model.MenuModuleID ?? (object)DBNull.Value },
            { "@Name", model.Name },
            { "@MenuSymbol", model.MenuSymbol ?? (object)DBNull.Value },
            { "@Sequence", model.Sequence },
            { "@RecType", model.RecType },
            { "@CreatedBy", model.CreatedBy ?? (object)DBNull.Value }
        };

            // Execute SP with TVP
            return _dbHelper.ExecuteSP_WithTableType_ReturnInt(
                "sp_SaveMenuWithModules",
                "@Modules",
                "ModuleTableType",
                dtModules,
                parameters
            );
        }
        public MenuModuleEditModel GetMenuModuleById(int menuId)
        {
            var ds = _dbHelper.ExecuteSP_ReturnDataSet("sp_GetMenuWithModulesById",
                new Dictionary<string, object> { { "@MenuModuleID", menuId } });

            if (ds == null || ds.Tables.Count < 2)
                return null;

            var menuTable = ds.Tables[0];
            var moduleTable = ds.Tables[1];

            if (menuTable.Rows.Count == 0)
                return null;

            var row = menuTable.Rows[0];

            var menu = new MenuModuleEditModel
            {
                MenuID = Convert.ToInt32(row["MenuID"]),
                MenuName = row["MenuName"].ToString(),
                MenuSeq = row["MenuSeq"] == DBNull.Value ? 0 : Convert.ToInt32(row["MenuSeq"]),
                MenuSymbol = row["MenuSymbol"]?.ToString(),
                Modules = new List<ModuleItem>()
            };

            foreach (DataRow mod in moduleTable.Rows)
            {
                menu.Modules.Add(new ModuleItem
                {
                    ModuleID = Convert.ToInt32(mod["ModuleID"]),
                    Name = mod["Name"].ToString(),
                    Url = mod["Url"]?.ToString(),
                    Sequence = mod["Sequence"] == DBNull.Value ? 0 : Convert.ToInt32(mod["Sequence"]),
                    ParentID = mod["ParentID"] == DBNull.Value ? 0 : Convert.ToInt32(mod["ParentID"])
                });
            }

            return menu;
        }


        public void DeleteMenu(int menuId)
        {
            _dbHelper.ExecuteSP_ReturnInt("sp_DeleteMenuWithModules", new Dictionary<string, object> { { "@MenuModuleID", menuId } });
        }

    }
}
