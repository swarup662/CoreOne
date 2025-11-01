using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserNotificationController : ControllerBase
    {
        private readonly IUserNotificationRepository _notificationRepo;

        public UserNotificationController(IUserNotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        [HttpPost("GetUserNotifications")]
        public IActionResult GetUserNotifications([FromBody] int userId)
        {
            var dt = _notificationRepo.GetUserNotifications(userId);

            var list = dt.AsEnumerable().Select(row => new UserNotification
            {
                NotificationID = row.Field<int>("NotificationID"),
                UserID = row.Field<int>("UserID"),
                Title = row.Field<string?>("Title") ?? string.Empty,
                Message = row.Field<string?>("Message") ?? string.Empty,
                IsRead = row.Field<int>("IsRead"),
                IsActive = row.Field<int>("IsActive"),
                StartDateTime = row.Field<DateTime?>("StartDateTime"),
                EndDateTime = row.Field<DateTime?>("EndDateTime"),
                CreatedBy = row.Field<int?>("CreatedBy"),
                CreatedDateTime = row.Field<DateTime>("CreatedDateTime"),
                UpdatedBy = row.Field<int?>("UpdatedBy"),
                UpdatedDateTime = row.Field<DateTime?>("UpdatedDateTime")
            }).ToList();

            return Ok(list);
        }



  
        // ✅ Mark as read
        [HttpPost("MarkAsRead")]
        public IActionResult MarkAsRead([FromBody] int notificationId)
        {
            int result = _notificationRepo.MarkAsRead(notificationId);
            return Ok(result > 0 ? "Marked as read" : "Error updating notification");
        }

        // ✅ Delete notification
        [HttpPost("DeleteNotification")]
        public IActionResult DeleteNotification([FromBody] int notificationId)
        {
            int result = _notificationRepo.DeleteNotification(notificationId);
            return Ok(result > 0 ? "Deleted successfully" : "Error deleting notification");
        }


        [HttpPost("SaveUserNotification")]
        public IActionResult SaveUserNotification([FromBody] UserNotification model)
        {
            try
            {
                int newId = _notificationRepo.SaveUserNotification(model);

                if (newId > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Notification saved successfully.",
                        notificationId = newId
                    });
                }

                return Ok(new { success = false, message = "Failed to save notification." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Exception: " + ex.Message
                });
            }
        }



    }
}
