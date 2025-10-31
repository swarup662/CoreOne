using CoreOne.DOMAIN.Models;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IUserNotificationRepository
    {
        DataTable GetUserNotifications(int userId);
        int AddNotification(UserNotification model);
        int MarkAsRead(int notificationId);
        int DeleteNotification(int notificationId);
    }
}
