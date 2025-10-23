using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class UserCreation
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string UserName { get; set; } = string.Empty;

  
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please select a mail type.")]
        public int? MailTypeID { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public int? RoleID { get; set; }

        public string? RoleName { get; set; }

        [Required(ErrorMessage = "Please select gender.")]
        public int? GenderID { get; set; }

        public string? GenderName { get; set; }

        // Optional image upload fields
        //[Required(ErrorMessage = "Please upload a file.")]
        public string? PhotoPath { get; set; }
        public string? PhotoName { get; set; }

        public int? ActiveFlag { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }

    public class UserCreationPagedResponse
    {
        public List<UserCreation> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? Search { get; set; }
        public string? SearchCol { get; set; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
            : 0;
    }

    public class UserCreationRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? SearchCol { get; set; }
        public string? Status { get; set; }
    }


    public class UserCreationDTO
    {
        public int? UserID { get; set; }

        
        public string? UserName { get; set; } = string.Empty;


        public string? PasswordHash { get; set; } = string.Empty;

        
        public string? Email { get; set; }

       
        public int? MailTypeID { get; set; }

        public string? PhoneNumber { get; set; }

       
        public int? RoleID { get; set; }

        public string? RoleName { get; set; }

       
        public int? GenderID { get; set; }

        public string? GenderName { get; set; }

        // Optional image upload fields
        //[Required(ErrorMessage = "Please upload a file.")]
        public string? PhotoPath { get; set; }
        public string? PhotoName { get; set; }

        public int? ActiveFlag { get; set; }

        public int? CreatedBy { get; set; }

        //public DateTime? CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        //public DateTime? UpdatedDate { get; set; }
    }
}
