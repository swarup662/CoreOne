using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class UserNotification
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int NotificationTypeID { get; set; }
        public int IsRead { get; set; }        // 0 = Unread, 1 = Read
        public int IsActive { get; set; }      // 1 = Active, 0 = Inactive
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }



    public class Notification_Dropdown
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

}
