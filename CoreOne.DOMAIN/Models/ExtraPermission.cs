using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class ExtraPermission
    {
        // Role Info
        public int RoleID { get; set; }
        public string? RoleName { get; set; }

        //User
        public int UserID { get; set; }

        // Menu / Module Info
        public int? ParentMenuID { get; set; }
        public string? ParentMenuName { get; set; }
        public int? MenuModuleID { get; set; }
        public string? ModuleName { get; set; }

        // Action Info
        public int? ActionID { get; set; }
        public string? ActionName { get; set; }

        // Permission
        public bool HasPermission { get; set; }

        //log
        public bool CreatedBy { get; set; }
    }
}
