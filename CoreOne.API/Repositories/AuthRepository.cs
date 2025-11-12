using Azure.Core;
using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _cache;
        public AuthRepository(DBContext dbHelper, TokenService tokenService, IConfiguration config, IEmailHelper email, IDistributedCache cache)
        {
            _dbHelper = dbHelper;
            _tokenService = tokenService;
            _config = config;
            _email = email;
            _cache = cache; 
        }

        public (bool Success, string Message, User User, string Token, List<UserAccessViewModel> AccessList)
            Login(string userName, string password, string ipAddress,  string userAgent)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_Auth_login_GetUserDetailsByUserName]",
                new Dictionary<string, object> { { "@UserName", userName } });

            if (dt.Rows.Count == 0)
                return (false, "Invalid username or password", null, null, null);

            var row = dt.Rows[0];
            if (!PasswordHelper.VerifyPassword(password, row["PasswordHash"].ToString()))
                return (false, "Invalid username or password", null, null, null);

            var user = new User
            {
                UserID = Convert.ToInt32(row["UserID"]),
                UserName = row["UserName"].ToString(),
                Email = row["Email"]?.ToString(),
                MailTypeID = row["MailTypeID"] == DBNull.Value ? 0 : Convert.ToInt32(row["MailTypeID"]),
                PhoneNumber = row["PhoneNumber"]?.ToString(),
                IsInternal = row.Table.Columns.Contains("IsInternal") && Convert.ToInt32(row["IsInternal"]) == 1
            };

            // singleton login
            var singletonDT = _dbHelper.ExecuteSP_ReturnDataTable("sp_Auth_GetAppSetting",
                new Dictionary<string, object> { { "@SettingKey", "SingletonLogin" } });
            bool isSingleton = singletonDT.Rows.Count > 0 &&
                               singletonDT.Rows[0]["SettingValue"].ToString() == "true";

            if (isSingleton)
            {
                int activeSessions = _dbHelper.ExecuteSP_ReturnInt("sp_Auth_CheckActiveSession",
                    new Dictionary<string, object> { { "@UserID", user.UserID } });
                if (activeSessions > 0)
                    return (false, "User already logged in elsewhere", null, null, null);
            }

            // get companies + apps
            string proc = user.IsInternal
                ? "sp_Auth_GetUserCompaniesAndApplications"
                : "sp_Auth_GetExternalDefaultAccess";

            var accessDt = _dbHelper.ExecuteSP_ReturnDataTable(proc, new Dictionary<string, object> { { "@UserID", user.UserID } });
            List<UserAccessViewModel> accessList = new List<UserAccessViewModel>();

            foreach (DataRow r in accessDt.Rows)
            {
                accessList.Add(new UserAccessViewModel
                {
                    CompanyID = Convert.ToInt32(r["CompanyID"]),
                    CompanyName = r["CompanyName"].ToString(),
                    ApplicationID = Convert.ToInt32(r["ApplicationID"]),
                    ApplicationName = r["ApplicationName"].ToString(),
                    RoleID = Convert.ToInt32(r["RoleID"]),
                    RoleName = r["RoleName"].ToString(),
                    ColorCode = r.Table.Columns.Contains("ColorCode") ? r["ColorCode"].ToString() : "#3498db",  // Default if missing
                    Icon = r.Table.Columns.Contains("Icon") ? r["Icon"].ToString() : "default-icon"
                });
            }
            
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertActivityLog", new Dictionary<string, object>
            {
                {"@UserID", user.UserID},
                  {"@RoleID", 0},
                {"@ActivityDescription", "Login successfull"},
                {"@IPAddress", ipAddress?? "0.0.0.0"},
                {"@DeviceInfo", userAgent},  // ✅ New field
                {"@CreatedBy", user.UserID}
            });

            if (!user.IsInternal)
            {
                string baseToken = _tokenService.GenerateUserToken(user, accessList);
                _dbHelper.ExecuteSP_ReturnInt("sp_Auth_InsertUserSession",
                    new Dictionary<string, object> { { "@UserID", user.UserID }, { "@Token", baseToken } });
                return (true, "Login successful", user, baseToken, accessList);
            }
            return (true, "Login successful", user, null, accessList);
        }

        public (bool Ok, string Message, string RedirectUrl)
        CreateCacheKeyAndGetRedirectUrl(int userId, int companyId, int appId, int roleId, string? sourceIp , string urlType = "domain")
        {
            string cacheKey = Guid.NewGuid().ToString("N");

            var payload = new
            {
                userId,
                companyId,
                appId,
                roleId,
                sourceIp,
                exp = DateTime.UtcNow.AddMinutes(2)
            };

            _cache.SetString(cacheKey, JsonConvert.SerializeObject(payload),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                });

            // Get App URL from DB
            var appDt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetAppUrlById", new Dictionary<string, object>
    {
        { "@ApplicationID", appId }
    });

            if (appDt.Rows.Count == 0)
                return (false, "Application not found", null);

            string domainUrl = appDt.Rows[0]["DomainUrl"]?.ToString();
            string ipUrl = appDt.Rows[0]["IpUrl"]?.ToString();
            string localUrl = appDt.Rows[0]["LocalUrl"]?.ToString();

            string appUrl = urlType?.ToLower() switch
            {
                "ipport" => ipUrl,
                "local" => localUrl,
                "domain" => domainUrl,
                _ => domainUrl ?? ipUrl
            };

            if (string.IsNullOrEmpty(appUrl))
                return (false, "App URL not available for the selected type", null);

            string redirectUrl = $"{appUrl.TrimEnd('/')}/auth/consume?OAuth={cacheKey}";
            return (true, "OK", redirectUrl);
        }


        public (bool Ok, string Message, string Token)
    ExchangeCacheKeyForToken(string cacheKey, int appId, string? callerIp, string userAgent)
        {
            // 1️⃣ Fetch and validate cache key
            var json = _cache.GetString(cacheKey);
            if (string.IsNullOrEmpty(json))
                return (false, "Cache key expired or invalid", null);

            dynamic data = JsonConvert.DeserializeObject(json);
            if (DateTime.UtcNow > (DateTime)data.exp)
            {
                _cache.Remove(cacheKey);
                return (false, "Expired cache key", null);
            }

            int userId = (int)data.userId;
            int companyId = (int)data.companyId;
            int roleId = (int)data.roleId;
            int applicationId = (int)data.appId;

            // 2️⃣ Fetch user details
            var userDt = _dbHelper.ExecuteSP_ReturnDataTable("sp_Auth_GetUserDetailsById",
                new Dictionary<string, object> { { "@UserID", userId } });

            if (userDt.Rows.Count == 0)
                return (false, "User not found", null);

            var row = userDt.Rows[0];
            var user = new User
            {
                UserID = userId,
                UserName = row["UserName"].ToString(),
                Email = row["Email"]?.ToString(),
                IsInternal = Convert.ToInt32(row["IsInternal"]) == 1
            };
            string proc = user.IsInternal
             ? "sp_Auth_GetUserCompaniesAndApplications"
             : "sp_Auth_GetExternalDefaultAccess";

            var accessDt = _dbHelper.ExecuteSP_ReturnDataTable(proc, new Dictionary<string, object> { { "@UserID", user.UserID } });
            List<UserAccessViewModel> accessList = new List<UserAccessViewModel>();

            foreach (DataRow r in accessDt.Rows)
            {
                accessList.Add(new UserAccessViewModel
                {
                    CompanyID = Convert.ToInt32(r["CompanyID"]),
                    CompanyName = r["CompanyName"].ToString(),
                    ApplicationID = Convert.ToInt32(r["ApplicationID"]),
                    ApplicationName = r["ApplicationName"].ToString(),
                    RoleID = Convert.ToInt32(r["RoleID"]),
                    RoleName = r["RoleName"].ToString(),
                    ColorCode = r.Table.Columns.Contains("ColorCode") ? r["ColorCode"].ToString() : "#3498db",  // Default if missing
                    Icon = r.Table.Columns.Contains("Icon") ? r["Icon"].ToString() : "default-icon"
                });
            }



            // 4️⃣ Generate unified user token
            string token = _tokenService.GenerateUserToken(user, accessList);

            // 5️⃣ Remove cache key (one-time use)
            _cache.Remove(cacheKey);

            // 6️⃣ Log activity
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertActivityLog", new Dictionary<string, object>
                {
                    {"@UserID", userId},
                     {"@RoleID", 0},
                    {"@ActivityDescription", $"Issued unified token for AppID:{applicationId}, CompanyID:{companyId}"},
                    {"@IPAddress", callerIp ?? "0.0.0.0"},
                    {"@DeviceInfo", userAgent},  // ✅ New field
                    {"@CreatedBy", userId}
                });

            return (true, "OK", token);
        }


        public int Logout(int userID, string ipAddress, string userAgent)
        {
            // Insert logout activity
            _dbHelper.ExecuteSP_ReturnInt("sp_InsertActivityLog", new Dictionary<string, object>
            {
                {"@UserID", userID },
                  {"@RoleID", 0},
                {"@ActivityDescription", "Logout" },
                {"@IPAddress", ipAddress },
                {"@DeviceInfo", userAgent},  // ✅ New field
                {"@CreatedBy", userID }
            });

            // Delete session
            return _dbHelper.ExecuteSP_ReturnInt("sp_Auth_DeleteUserSession", new Dictionary<string, object>
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
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_Auth_ChangePassword_GetUserById]", new() { { "@UserID", userId } });
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

            int res = _dbHelper.ExecuteSP_ReturnInt("[sp_Auth_ChangePassword_PasswordChange]", p);

            return new PasswordValidationResponse
            {
                Success = res == 1,
                Message = res == 1 ? "Password changed successfully" : "Error changing password"
            };
        }

        // 🔹 Forgot Password
        public PasswordValidationResponse ForgotPassword(string email)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_Auth_ForgotPassword_GeneratePasswordResetToken", new() { { "@Email", email } });

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

            DataSet ds= _dbHelper.ExecuteSP_ReturnDataSet("[sp_Auth_ForgotPassword_GetMailSetupByMailPurposeId]", p);

            DataTable mailSetupDt = ds.Tables[0];
            DataTable toDt = ds.Tables.Count > 1 ? ds.Tables[1] : null;
            return (mailSetupDt, toDt);
                
        }

        // 🔹 Validate Reset Token
        public PasswordValidationResponse ValidateResetToken(string token)
        {
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_Auth_ResetPassword_ValidateResetToken]", new() { { "@Token", token } });

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

            int res = _dbHelper.ExecuteSP_ReturnInt("[sp_Auth_ResetPassword_PasswordReset]", p);

            return new PasswordValidationResponse
            {
                Success = res == 1,
                Message = res == 1 ? "Password reset successful" : "Invalid or expired token"
            };
        }
        #endregion











    }
}
