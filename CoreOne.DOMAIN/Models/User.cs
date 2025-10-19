using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; } // store plain here if no hashing
        public string Email { get; set; }
        public int? MailTypeID { get; set; }
        public string PhoneNumber { get; set; }
       
        public int RoleID { get; set; }

        public string RoleName { get; set; }
        public bool ActiveFlag { get; set; }
        public int CreatedBy { get; set; }
       
    }
}
