using CoreOne.API.Helpers;
using CoreOne.API.Interfaces;

using CoreOne.API.Services;
using CoreOne.COMMON.Models;
using Newtonsoft.Json;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DBHelper _dbHelper;
        private readonly TokenService _tokenService;

        public AuthRepository(DBHelper dbHelper, TokenService tokenService)
        {
            _dbHelper = dbHelper;
            _tokenService = tokenService;
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

    }
}
