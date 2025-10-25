using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class ActionCreationRepository : IActionCreationRepository
    {
        private readonly DBContext _dbHelper;

        public ActionCreationRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<List<ActionCreationDto>> GetActions()
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("USP_GetActions");

            List<ActionCreationDto> result = new();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ActionCreationDto
                {
                    ActionID = Convert.ToInt32(row["ActionID"]),
                    ActionName = row["ActionName"].ToString(),
                    Description = row["Description"].ToString(),
                    ActiveFlag = row["ActiveFlag"] != DBNull.Value ? Convert.ToInt32(row["ActiveFlag"]) : 0,
                    CreatedDate = Convert.ToDateTime(row["CreatedDate"]).ToString("dd-MM-yyyy")
                });
            }

            return result;
        }


        public ActionCreationDto? GetActionById(int actionId)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_Action_CRUD", new Dictionary<string, object>
            {
                { "@RecType", "GETBYID" },
                { "@ActionID", actionId }
            });

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new ActionCreationDto
            {
                ActionID = Convert.ToInt32(row["ActionID"]),
                ActionName = row["ActionName"]?.ToString() ?? string.Empty,
                Description = row["Description"]?.ToString() ?? string.Empty,
                CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                CreatedDate = row["CreatedDate"]?.ToString() ?? string.Empty,
                ActiveFlag = row["ActiveFlag"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["ActiveFlag"]),
            };
        }




    }
}
