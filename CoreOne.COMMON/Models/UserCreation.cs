using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.COMMON.Models
{
    public class UserCreation
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(255, ErrorMessage = "Password cannot exceed 255 characters.")]
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
        [Required(ErrorMessage = "Please upload a file.")]
        public string? PhotoPath { get; set; }
        public string? PhotoName { get; set; }

        public int? ActiveFlag { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
