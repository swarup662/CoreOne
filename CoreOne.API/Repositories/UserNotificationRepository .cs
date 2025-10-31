using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
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
        {"@Title", model.Title},
        {"@Message", model.Message},
        {"@Type", model.Type},
        {"@IsRead", model.IsRead},
        {"@IsActive", model.IsActive},
        {"@StartDateTime", model.StartDateTime},
        {"@EndDateTime", model.EndDateTime},
        {"@CreatedBy", model.CreatedBy},
        {"@UpdatedBy", model.UpdatedBy}
    };

            return _dbHelper.ExecuteSP_ReturnInt("sp_AddOrUpdateUserNotification", parameters);
        }



        public int DeleteNotification(int notificationId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@NotificationID", notificationId}
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_DeleteUserNotification", parameters);
        }
    }
}
