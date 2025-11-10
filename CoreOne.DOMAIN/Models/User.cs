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




        // 🔹 Key = RoleID, Value = RoleName
        public List<Roles>? Roles { get; set; } 
        public Boolean IsInternal { get; set; }

      
        public bool ActiveFlag { get; set; }
        public int CreatedBy { get; set; }
        public List<UserAccessViewModel>? UserAccessList { get; set; }


    }
public class Roles
{
    public int RoleID { get; set; }
    public string? RoleName{ get; set; }
}

public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class CreateCacheKeyRequest
    {
        public int UserID { get; set; }
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int RoleID { get; set; }
        public string UrlType { get; set; } = "domain";
    }

    public class ExchangeCacheKeyRequest
    {
        public string CacheKey { get; set; }
        public int ApplicationID { get; set; }
        public string AppSecret { get; set; }
    }
    public class UserAccessViewModel
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public string ColorCode { get; set; }     // Example: "#3498db"
        public string Icon { get; set; }
    }

    public class AppLaunchRequest
    {
        public int UserID { get; set; }
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int RoleID { get; set; }
        // NEW: Choose which app URL type to use ("domain" or "port")
        public string UrlType { get; set; } = "domain";
    }
}
