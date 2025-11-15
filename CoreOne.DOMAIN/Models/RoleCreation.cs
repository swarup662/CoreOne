using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class RoleCreation
    {
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Role Name is required.")]
        [StringLength(20, ErrorMessage = "Role Name cannot exceed 100 characters.")]
        public string RoleName { get; set; } 

        [Required(ErrorMessage = "Role Description is required.")]
        [StringLength(100, ErrorMessage = "Role Description cannot exceed 250 characters.")]
        public string RoleDescription { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public int? ActiveFlag { get; set; }

        public int? DisplayOn { get; set; }

        public DateTime? UpdatedDate { get; set; }

    }
    public class DeleteRoleCreation
    {
        public int? RoleID { get; set; }

        public int? CreatedBy { get; set; }
        

    }


    public class RoleCreationsPagedResponse
    {
        public List<RoleCreation> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? Search { get; set; }
        public string? SearchCol { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalRecords / (double)PageSize) : 0;
    }



    public class RoleCreationsRequest
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? SearchCol { get; set; }
        public int ApplicationID { get; set; } = 1;
        public int CurrentUserID { get; set; }   // <-- NEW
    }


}
