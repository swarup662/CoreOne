using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly DBContext _dbHelper;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IEmailHelper _email;

        public AuthenticationRepository(DBContext dbHelper, TokenService tokenService, IConfiguration config, IEmailHelper email)
        {
            _dbHelper = dbHelper;
            _tokenService = tokenService;
            _config = config;
            _email = email;
        }

        public (bool Success, string Message, User User, string Token) Login(string userName, string password, string ipAddress)
        {
            // 1️⃣ Get user by username
            var parameters = new Dictionary<string, object> { { "@UserName", userName } };
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserByUserName", parameters);

            if (dt.Rows.Count == 0)
                return (false, "Invalid username or password", null, null);

            var row = dt.Rows[0];
            string storedHash = row["PasswordHash"].ToString();

            // 2️⃣ Check password hash
            if (!PasswordHelper.VerifyPassword(password, storedHash))
                return (false, "Invalid username or password", null, null);

            int userID = Convert.ToInt32(row["UserID"]);
            int roleID = Convert.ToInt32(row["RoleID"]);
            string uName = row["UserName"].ToString();
            string RoleName = row["RoleName"].ToString();
            string Email = row["Email"].ToString();
            int MailTypeID = Convert.ToInt32(row["MailTypeID"]);
            string PhoneNumber = row["PhoneNumber"].ToString();

            // 3️⃣ Check singleton login
            var singletonDT = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetAppSetting", new Dictionary<string, object>
            {
                {"@SettingKey", "SingletonLogin"}
            });
            bool isSingleton = singletonDT.Rows.Count > 0 && singletonDT.Rows[0]["SettingValue"].ToString() == "true";

            if (isSingleton)
            {
                int activeSessions = _dbHelper.ExecuteSP_ReturnInt("sp_CheckActiveSession", new Dictionary<string, object>
                {
                    {"@UserID", userID}
                });
                if (activeSessions > 0)
                    return (false, "User already logged in elsewhere", null, null);
            }

            // 4️⃣ Generate JWT token
            var user = new User { UserID = userID, 
                UserName = uName, 
                RoleID = roleID ,
                RoleName =RoleName,
                Email =Email,
                MailTypeID = MailTypeID,
                PhoneNumber = PhoneNumber
            };
            string token = _tokenService.GenerateToken(user);

            // 5️⃣ Log login activity
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertActivityLog", new Dictionary<string, object>
            {
                {"@UserID", userID},
                {"@ActivityDescription", "Login"},
                {"@IPAddress", ipAddress},
                {"@CreatedBy", userID}
            });

            // 6️⃣ Insert session record
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertUserSession", new Dictionary<string, object>
            {
                {"@UserID", userID },
                {"@Token", token }
            });

            return (true, "Login successful", user, token);

        }


        public int Logout(int userID, string ipAddress)
        {
            // Insert logout activity
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertActivityLog", new Dictionary<string, object>
            {
                {"@UserID", userID },
                {"@ActivityDescription", "Logout" },
                {"@IPAddress", ipAddress },
                {"@CreatedBy", userID }
            });

            // Delete session
            return _dbHelper.ExecuteSP_ReturnInt("sp_DeleteUserSession", new Dictionary<string, object>
            {
                {"@UserID", userID }
            });
        }



        public int LogHttpError(LogHttpErrorRequest request)
        {
            var parameters = new Dictionary<string, object>
                {
                    { "@UserID", request.UserID ?? "0"},
                      { "@ErrorType", "API REQUEST CODE"},
                    { "@ErrorMessage", $"HTTP {request.StatusCode}" },
                    { "@StackTrace", "" },
                    { "@RequestPath", request.RequestPath },
                    { "@Headers", request.Headers }
                };
            return _dbHelper.ExecuteSP_ReturnInt("sp_ErrorLogs", parameters);
        }

        public int LogException(LogExceptionRequest request)
        {
            var parameters = new Dictionary<string, object>
                {
                    { "@UserID", request.UserID ?? "0"},
                      { "@ErrorType", "UI EXCEPTION"},
                    { "@ErrorMessage", request.ErrorMessage },
                    { "@StackTrace", request.StackTrace },
                    { "@RequestPath", request.RequestPath },
                    { "@Headers", request.Headers }
                };
             return _dbHelper.ExecuteSP_ReturnInt("sp_ErrorLogs", parameters);
        }







        #region Forgot-Change-Password

        public (bool Success, string Message) ChangePassword(int? userId, string currentPwd, string newPwd)
        {
            // Fetch user
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_GetUserByIdChangePassword]", new() { { "@UserID", userId } });
            if (dt.Rows.Count == 0) return (false, "User not found");

            string storedHash = dt.Rows[0]["PasswordHash"].ToString();
            if (!PasswordHelper.VerifyPassword(currentPwd, storedHash))
                return (false, "Invalid current password");

            string newHash = PasswordHelper.HashPassword(newPwd);

            // Prevent reusing same password
            if (PasswordHelper.VerifyPassword(newPwd, storedHash))
                return (false, "New password cannot be same as old one");

            var p = new Dictionary<string, object>
    {
        { "@UserID", userId },
        { "@CurrentPassword", storedHash },
        { "@NewPassword", newHash }
    };
            int res = _dbHelper.ExecuteSP_ReturnInt("sp_ChangePassword", p);

            return res == 1 ? (true, "Password changed successfully") : (false, "Error changing password");
        }

        public (bool Success, string Message) ForgotPassword(string email)
        {
            // Generate token and get MailTypeID from SP
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GeneratePasswordResetToken", new() { { "@Email", email } });
            if (dt.Rows.Count == 0) return (false, "Email not registered");

            string token = dt.Rows[0]["Token"].ToString();
            int mailType = Convert.ToInt32(dt.Rows[0]["MailTypeID"]);

            // Map MailTypeID → provider name
            string provider = mailType switch
            {
                1 => "Gmail",
                2 => "Yahoo",
                3 => "Outlook",
                _ => "Gmail" // default fallback
            };

            // Build reset link
            string uiBase = _config["BaseUrlUI"];
            string link = $"{uiBase}/Account/ResetPassword?token={token}";
            string subject = "Password Reset (valid for 10 minutes)";
            string body = $@"
        <p>Hello,</p>
        <p>You requested a password reset. Click the link below to reset your password:</p>
        <p><a href='{link}' target='_blank'>Reset Password</a></p>
        <p>This link will expire in <strong>10 minutes</strong>.</p>
        <p>Thanks,<br/>CoreOne Support Team</p>
    ";

            // Send email asynchronously
            _ = _email.SendEmailAsync(provider, email, subject, body);
            return (true, "Password reset link sent to your email.");
        }

        public (bool Success, string Message, int UserID) ValidateResetToken(string token)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_ValidateResetToken", new() { { "@Token", token } });
            if (dt.Rows.Count == 0) return (false, "Invalid or expired token", 0);

            int userId = Convert.ToInt32(dt.Rows[0]["UserID"]);
            return (true, "Valid token", userId);
        }

        public (bool Success, string Message) ResetPassword(int? userId, string newPwd, string token)
        {
            string hash = PasswordHelper.HashPassword(newPwd);
            var p = new Dictionary<string, object>
    {
        { "@UserID", userId },
        { "@NewPassword", hash },
        { "@Token", token }
    };

            int res = _dbHelper.ExecuteSP_ReturnInt("sp_ResetPassword", p);
            return res == 1 ? (true, "Password reset successful") : (false, "Invalid or expired token");
        }

        #endregion











    }
}
