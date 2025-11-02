using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DBContext _dbHelper;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IEmailHelper _email;

        public AuthRepository(DBContext dbHelper, TokenService tokenService, IConfiguration config, IEmailHelper email)
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

        public PasswordValidationResponse ChangePassword(int? userId, string currentPwd, string newPwd)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_GetUserByIdChangePassword]", new() { { "@UserID", userId } });
            if (dt.Rows.Count == 0)
                return new PasswordValidationResponse { Success = false, Message = "User not found" };

            string storedHash = dt.Rows[0]["PasswordHash"].ToString();

            if (!PasswordHelper.VerifyPassword(currentPwd, storedHash))
                return new PasswordValidationResponse { Success = false, Message = "Invalid current password" };

            if (PasswordHelper.VerifyPassword(newPwd, storedHash))
                return new PasswordValidationResponse { Success = false, Message = "New password cannot be same as old one" };

            string newHash = PasswordHelper.HashPassword(newPwd);

            var p = new Dictionary<string, object>
        {
            { "@UserID", userId },
            { "@CurrentPassword", storedHash },
            { "@NewPassword", newHash }
        };

            int res = _dbHelper.ExecuteSP_ReturnInt("sp_ChangePassword", p);

            return new PasswordValidationResponse
            {
                Success = res == 1,
                Message = res == 1 ? "Password changed successfully" : "Error changing password"
            };
        }

        // 🔹 Forgot Password
        public PasswordValidationResponse ForgotPassword(string email)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GeneratePasswordResetToken", new() { { "@Email", email } });

            if (dt.Rows.Count == 0)
                return new PasswordValidationResponse { Success = false, Message = "Email not found" };

            string token = dt.Rows[0]["Token"].ToString();
            int mailType = Convert.ToInt32(dt.Rows[0]["MailTypeID"]);

            

            var (mailSetupDt, toDt) = GetMailSetupByPurpose(1);
            string uiBase = _config["BaseUrlUI"];
            string link = $"{uiBase}Account/ResetPassword?token={token}";
          
            if (mailSetupDt.Rows.Count > 0)
            {
                string subject = mailSetupDt.Rows[0]["Subject"].ToString();
                string FromMail = mailSetupDt.Rows[0]["FromMail"].ToString();
                string FromMailPassword = mailSetupDt.Rows[0]["FromMailPassword"].ToString();
                int MailTypeId = Convert.ToInt32(mailSetupDt.Rows[0]["MailTypeId"]);
                string body = mailSetupDt.Rows[0]["Body"].ToString().Replace("{link}", link);
                // send email using same logic as before
                _ = _email.SendEmailAsyncToIndividual(MailTypeId, email, subject, body, FromMail, FromMailPassword);
            }

          

            return new PasswordValidationResponse
            {
                Success = true,
                Message = "Password reset link sent to your email."
            };
        }

        public (DataTable MailSetup, DataTable MailTo) GetMailSetupByPurpose(int mailPurposeId)
        {
            var p = new Dictionary<string, object>
        {
            { "@MailPurposeID", mailPurposeId },
            { "@UserId", 0 }
            
        };

            DataSet ds= _dbHelper.ExecuteSP_ReturnDataSet("sp_GetMailSetupByPurpose", p);

            DataTable mailSetupDt = ds.Tables[0];
            DataTable toDt = ds.Tables.Count > 1 ? ds.Tables[1] : null;
            return (mailSetupDt, toDt);
                
        }

        // 🔹 Validate Reset Token
        public PasswordValidationResponse ValidateResetToken(string token)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_ValidateResetToken", new() { { "@Token", token } });

            if (dt.Rows.Count == 0)
                return new PasswordValidationResponse { Success = false, Message = "Invalid or expired token" };

            return new PasswordValidationResponse
            {
                Success = true,
                Message = "Valid token",
                UserID = Convert.ToInt32(dt.Rows[0]["UserID"]),
                ExpiresAt = Convert.ToDateTime(dt.Rows[0]["ExpiresAt"])
            };
        }

        // 🔹 Reset Password
        public PasswordValidationResponse ResetPassword(int? userId, string newPwd, string token)
        {
            string hash = PasswordHelper.HashPassword(newPwd);

            var p = new Dictionary<string, object>
        {
            { "@UserID", userId },
            { "@NewPassword", hash },
            { "@Token", token }
        };

            int res = _dbHelper.ExecuteSP_ReturnInt("sp_ResetPassword", p);

            return new PasswordValidationResponse
            {
                Success = res == 1,
                Message = res == 1 ? "Password reset successful" : "Invalid or expired token"
            };
        }
        #endregion











    }
}
