using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class RolePermission
    {
        // Role Info
        public int RoleID { get; set; }
        public string? RoleName { get; set; }

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
    }


    public class RolePermissionGridRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
    }

 
    public class RolePermissionGridPagedResponse
    {
        public List<RolePermission> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? Search { get; set; }
    }


}
