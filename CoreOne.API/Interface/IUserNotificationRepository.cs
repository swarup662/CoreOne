using CoreOne.DOMAIN.Models;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IUserNotificationRepository
    {
        DataTable GetUserNotifications(int userId);
     
        int MarkAsRead(int notificationId);
        int DeleteNotification(int notificationId);
        int SaveUserNotification(UserNotification model);
        DataTable GetUserNotificationGrid(
             int pageSize,
             int pageNumber,
             string? search,
             string? sortColumn,
             string? sortDir,
             string? searchCol,
             int? createdBy
         );
        DataRow? GetUserNotificationById(int notificationId);
    }
}
