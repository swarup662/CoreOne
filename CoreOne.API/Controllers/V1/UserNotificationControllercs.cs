using CoreOne.API.Interfaces;
using CoreOne.API.Repositories;
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




        [HttpPost("GetUserNotificationGrid")]
        public IActionResult GetUserNotificationGrid([FromBody] UserNotificationRequest request)
        {
            if (request == null)
                return BadRequest("Request is required.");

            var dt = _notificationRepo.GetUserNotificationGrid(
                request.PageSize,
                request.PageNumber,
                request.Search,
                request.SortColumn,
                request.SortDir,
                request.SearchCol,
                request.CreatedBy
            );

            var notifications = new List<UserNotificationDto>();
            foreach (DataRow row in dt.Rows)
            {
                notifications.Add(new UserNotificationDto
                {
                    NotificationID = Convert.ToInt32(row["NotificationID"]),
                    UserID = Convert.ToInt32(row["UserID"]),
                    UserName = row["UserName"]?.ToString() ?? string.Empty,
                    Title = row["Title"]?.ToString() ?? string.Empty,
                    Message = row["Message"]?.ToString() ?? string.Empty,
                    Type = row["Type"]?.ToString() ?? string.Empty,
                    IsRead = row["IsRead"] != DBNull.Value && Convert.ToBoolean(row["IsRead"]),
                    StartDateTime = row["StartDateTime"] == DBNull.Value ? null : Convert.ToDateTime(row["StartDateTime"]),
                    CreatedDateTime = row["CreatedDateTime"] == DBNull.Value ? null : Convert.ToDateTime(row["CreatedDateTime"])
                });
            }

            int totalRecords = 0;
            if (dt.Rows.Count > 0 && dt.Columns.Contains("TotalRecords"))
                totalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

            var response = new UserNotificationPagedResponse
            {
                Items = notifications,
                TotalRecords = totalRecords,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortColumn = request.SortColumn,
                SortDir = request.SortDir,
                Search = request.Search,
                SearchCol = request.SearchCol
            };

            return Ok(response);
        }



        [HttpGet("GetUserNotificationById")]
        public IActionResult GetUserNotificationById(int id)
        {
            var row = _notificationRepo.GetUserNotificationById(id);
            if (row == null)
                return NotFound();

            var dto = new UserNotificationEdit
            {
                NotificationID = Convert.ToInt32(row["NotificationID"]),
                UserID = Convert.ToInt32(row["UserID"]),
                UserName = row["UserName"]?.ToString(),
                Title = row["Title"]?.ToString(),
                Message = row["Message"]?.ToString(),
                NotificationTypeID = Convert.ToInt32(row["NotificationTypeID"]),
                IsRead = Convert.ToBoolean(row["IsRead"]),
                StartDateTime = row["StartDateTime"] == DBNull.Value ? null : (DateTime?)row["StartDateTime"],
                EndDateTime = row["EndDateTime"] == DBNull.Value ? null : (DateTime?)row["EndDateTime"],
                CreatedDateTime = row["CreatedDateTime"] == DBNull.Value ? null : (DateTime?)row["CreatedDateTime"]
            };

            return Ok(dto);
        }










        [HttpPost("DeleteUserNotification")]
        public IActionResult DeleteUserNotification([FromBody] DeleteUserNotificationRequest act)
        {
            if (act.NotificationID <= 0)
                return BadRequest(new { success = false, message = "Invalid NotificationID." });

            int result = _notificationRepo.DeleteUserNotification(act.NotificationID, act.CreatedBy);

            if (result > 0)
                return Ok(new { success = true, message = "Notification deleted successfully." });
            else
                return NotFound(new { success = false, message = "Notification not found or already deleted." });
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


        [HttpPost("SaveUserNotificationBulk")]
        public IActionResult SaveUserNotificationBulk([FromBody] List<UserNotification> notifications)
        {
            try
            {
                if (notifications == null || notifications.Count == 0)
                    return BadRequest(new { success = false, message = "No notifications provided." });

                var result = _notificationRepo.SaveUserNotificationBulk(notifications);

                return Ok(new
                {
                    success = true,
                    message = "Bulk notifications saved successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Server Error: {ex.Message}"
                });
            }
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
