using CoreOne.API.Helpers;
using CoreOne.API.Interfaces;
using CoreOne.COMMON.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class UserCreationRepository : IUserCreationRepository
    {
        private readonly DBHelper _dbHelper;

        public UserCreationRepository(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetUsers(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol, string status)
        {
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;
            if (string.IsNullOrEmpty(status) )
            {
                status = null;
            }
            var parameters = new Dictionary<string, object>
            {
                {"@PageSize", pageSize},
                {"@PageNumber", pageNumber},
                {"@Search", (object?)search ?? DBNull.Value},
                {"@SearchCol", searchCol},
                {"@SortColumn", sortColumn},
                {"@SortDir", sortDir},
                {"@ActiveFlag", status}
            };

            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUsers", parameters);
        }

        public int SaveUser(string recType, UserCreation user)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@RecType", recType},
                {"@UserID", user.UserID},
                {"@UserName", user.UserName},
                {"@PasswordHash", user.PasswordHash},
                {"@Email", (object?)user.Email ?? DBNull.Value},
                {"@MailTypeID", (object?)user.MailTypeID ?? DBNull.Value},
                 {"@GenderID", (object?)user.GenderID ?? DBNull.Value},
                {"@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value},
                {"@RoleID", (object?)user.RoleID ?? DBNull.Value},
                {"@PhotoPath", (object?)user.PhotoPath ?? DBNull.Value},
                {"@PhotoName", (object?)user.PhotoName ?? DBNull.Value},
                {"@UserID_Action", user.CreatedBy ?? 0}
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_Users_CRUD", parameters);
        }

        public UserCreation? GetUserById(int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@UserID", userId}
            };

            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetUserById", parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new UserCreation
            {
                UserID = Convert.ToInt32(row["UserID"]),
                UserName = row["UserName"]?.ToString(),
                Email = row["Email"]?.ToString(),
                PhoneNumber = row["PhoneNumber"]?.ToString(),
                RoleID = row["RoleID"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["RoleID"]),
                MailTypeID = row["MailTypeID"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["MailTypeID"]),
                GenderID = row["GenderID"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["GenderID"]),
                PhotoPath = row["PhotoPath"]?.ToString(),
                PhotoName = row["PhotoName"]?.ToString(),
                CreatedDate = row["CreatedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["CreatedDate"]),
                UpdatedDate = row["UpdatedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["UpdatedDate"])
            };
        }

        public DataTable? GetRoles(int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@Rectype", "ROLE"},
                {"@UserID", userId}
            };

            DataTable dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_UserCreations_Dropdown", parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return dt;
        }

        public DataTable? GetGenders(int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@Rectype", "GENDER"},
                {"@UserID", userId}
            };

            DataTable dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_UserCreations_Dropdown", parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return dt;
        }
        public DataTable? GetMailtypes(int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@Rectype", "MAILTYPE"},
                {"@UserID", userId}
            };

            DataTable dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_UserCreations_Dropdown", parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return dt;
        }

    }
}
