using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class UserCreation
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string UserName { get; set; } = string.Empty;

        private string _passwordHash = string.Empty;

        [DataType(DataType.Password)]
        [CustomPasswordValidation] // Apply your custom validation
        public string PasswordHash
        {
            get => _passwordHash;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _passwordHash = string.Empty; // allow null or empty
                }
                else
                {
                    _passwordHash = value.Replace(" ", ""); // remove spaces automatically
                }
            }
        }
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

    // Your custom password validation attribute
    public class CustomPasswordValidation : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Access the parent model (UserCreation)
            var user = (UserCreation)validationContext.ObjectInstance;

            var password = value?.ToString() ?? string.Empty;

            // 🟢 If it's a new user (UserID == 0), password is mandatory
            if (user.UserID == 0 && string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult("Password is required for new users.");
            }

            // 🟡 If editing existing user (UserID > 0) and password empty, allow it
            if (user.UserID > 0 && string.IsNullOrWhiteSpace(password))
            {
                return ValidationResult.Success;
            }

            // ✅ Validate password strength (no special character required)
            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            if (!Regex.IsMatch(password, pattern))
            {
                return new ValidationResult("Password must be at least 8 characters long and include uppercase, lowercase, and numeric characters.");
            }

            return ValidationResult.Success;
        }
    }







    public class UserCreationEditDTO
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string UserName { get; set; } = string.Empty;

        private string _passwordHash = string.Empty;

        [DataType(DataType.Password)]
        [CustomEditPasswordValidation] // Apply your custom validation
        public string PasswordHash
        {
            get => _passwordHash;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _passwordHash = string.Empty; // allow null or empty
                }
                else
                {
                    _passwordHash = value.Replace(" ", ""); // remove spaces automatically
                }
            }
        }
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

    public class CustomEditPasswordValidation : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success; // null or empty is allowed

            var password = value.ToString()!;
            // ✅ Password rule:
            // - At least 8 chars
            // - At least 1 uppercase, 1 lowercase, 1 digit
            // - No special characters required
            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
            if (!Regex.IsMatch(password, pattern))
                return new ValidationResult("Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");

            return ValidationResult.Success;
        }
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
        public string? RecType { get; set; }
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
