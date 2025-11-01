using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class UserNotificationRepository : IUserNotificationRepository
    {
        private readonly DBContext _dbHelper;

        public UserNotificationRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }




        public DataTable GetUserNotificationGrid(
            int pageSize,
            int pageNumber,
            string? search,
            string? sortColumn,
            string? sortDir,
            string? searchCol,
            int? createdBy)
        {
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;
            sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "CreatedDateTime" : sortColumn;
            sortDir = string.IsNullOrWhiteSpace(sortDir) ? "DESC" : sortDir;

            var parameters = new Dictionary<string, object>
    {
        { "@PageSize", pageSize },
        { "@PageNumber", pageNumber },
        { "@Search", (object?)search ?? DBNull.Value },
        { "@SortColumn", sortColumn },
        { "@SortDir", sortDir },
        { "@SearchCol", (object?)searchCol ?? DBNull.Value },
        { "@CreatedBy", (object?)createdBy ?? DBNull.Value }
    };

            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserNotificationGrid", parameters);
        }










        public DataTable GetUserNotifications(int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@UserID", userId}
            };

            var ds = _dbHelper.ExecuteSP_ReturnDataSet("sp_GetUserNotifications", parameters);
            return ds.Tables[0];
        }




        public int MarkAsRead(int notificationId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@NotificationID", notificationId}
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_MarkNotificationAsRead", parameters);
        }
        public int SaveUserNotification(UserNotification model)
        {
            var parameters = new Dictionary<string, object>
    {
        {"@NotificationID", model.NotificationID},
        {"@UserID", model.UserID},
        {"@NotificationTypeID", model.NotificationTypeID},
        {"@Title", model.Title},
        {"@Message", model.Message},
        {"@IsRead", model.IsRead},
        {"@IsActive", model.IsActive},
        {"@StartDateTime", model.StartDateTime},
        {"@EndDateTime", model.EndDateTime},
        {"@CreatedBy", model.CreatedBy},
        {"@UpdatedBy", model.UpdatedBy}
    };

            return _dbHelper.ExecuteSP_ReturnInt("sp_AddOrUpdateUserNotification", parameters);
        }

        public DataRow? GetUserNotificationById(int notificationId)
        {
            var parameters = new Dictionary<string, object>
        {
            {"@NotificationID", notificationId}
        };

            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserNotificationById", parameters);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public int DeleteUserNotification(int notificationId, int createdBy)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@NotificationID", notificationId },
                { "@CreatedBy", createdBy }
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_DeleteUserNotification", parameters);
        }



    }
}
