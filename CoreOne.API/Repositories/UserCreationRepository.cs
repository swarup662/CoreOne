using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class UserCreationRepository : IUserCreationRepository
    {
        private readonly DBContext _dbHelper;

        public UserCreationRepository(DBContext dbHelper)
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
            string newHash = "";
            if (!String.IsNullOrEmpty(user.PasswordHash))
            {
                newHash=PasswordHelper.HashPassword(user.PasswordHash);
                
            }

            var parameters = new Dictionary<string, object>
            {
                {"@RecType", recType},
                {"@UserID", user.UserID},
                {"@UserName", user.UserName},
                {"@PasswordHash",newHash},
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


        public int UpdateUser(string recType, UserCreationEditDTO user)
        {
            string newHash = "";
            if (!String.IsNullOrEmpty(user.PasswordHash))
            {
                newHash = PasswordHelper.HashPassword(user.PasswordHash);

            }

            var parameters = new Dictionary<string, object>
            {
                {"@RecType", recType},
                {"@UserID", user.UserID},
                {"@UserName", user.UserName},
                {"@PasswordHash",newHash},
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



        public UserCreation? GetUserMyAccount(int userId)
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
                ActiveFlag = row["ActiveFlag"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["ActiveFlag"]),
                RoleName = row["RoleName"]?.ToString(),
                MailTypeID = row["MailTypeID"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["MailTypeID"]),
                GenderID = row["GenderID"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["GenderID"]),
                GenderName = row["GenderName"]?.ToString(),
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


        public async Task<int> SaveExtraPermissionAsync(int CreatedBy ,IEnumerable<ExtraPermission> extraPermissions)
        {
           
            int roleId = 0;
            var firstPermission = extraPermissions.FirstOrDefault();

            if (firstPermission != null)
            {
                
                 roleId = firstPermission.RoleID;
                // use userId and roleId here
            }
            var dt = new DataTable();
            dt.Columns.Add("RoleID", typeof(int));
            dt.Columns.Add("UserID", typeof(int));
            dt.Columns.Add("MenuModuleID", typeof(int));
            dt.Columns.Add("ActionID", typeof(int));
            dt.Columns.Add("ActiveFlag", typeof(int));


            foreach (var rp in extraPermissions)
            {
                int ActiveFlag = 0;
                if (rp.HasPermission)
                {
                    ActiveFlag = 1;
                }
                else
                {
                    ActiveFlag = 0;
                }
                dt.Rows.Add(

                    rp.RoleID,
                    rp.UserID,
                    rp.MenuModuleID,
                    rp.ActionID,
                    ActiveFlag

                );
            }

            var parameters = new Dictionary<string, object>
            {
                { "@RoleId", roleId },
                { "@UserID", CreatedBy }
            };

            var result = _dbHelper.ExecuteSP_WithTableType_ReturnInt("sp_SaveExtraPermissions", "Permissions", "ExtraPermissionTableType", dt, parameters);
            return await Task.FromResult(result);
        }


        // Extra Permissions by User Id
        public async Task<IEnumerable<ExtraPermission>> GetExtraPermissionByUserId(int UserId, int CreatedBy)
        {
            var parameters = new Dictionary<string, object> { 
                { "@UserId", UserId },
                 { "@CreatedBy", CreatedBy }
            };
            var dt = _dbHelper.ExecuteSP_ReturnDataTable("sp_GetExtraPermissionsByUserID", parameters);

            var list = new List<ExtraPermission>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ExtraPermission
                {
                    UserID = Convert.ToInt32(row["UserID"]),
                    MenuModuleID = Convert.ToInt32(row["MenuModuleID"]),
                    ModuleName = row["ModuleName"].ToString(),
                    ParentMenuID = row["ParentMenuID"] != DBNull.Value ? Convert.ToInt32(row["ParentMenuID"]) : null,
                    ParentMenuName = row["ParentMenuName"]?.ToString(),
                    ActionID = Convert.ToInt32(row["ActionID"]),
                    ActionName = row["ActionName"].ToString(),
                    HasPermission = Convert.ToBoolean(row["HasPermission"])
                });
            }

            return await Task.FromResult(list);
        }



        public int ActivateDeactivateUser(string recType, UserCreationDTO user)
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

        public DataTable GetNotficationDropdown()
        {
            // Only ActionID and ActionName
            return _dbHelper.ExecuteSP_ReturnDataTable("GetNotficationDropdown", new Dictionary<string, object>());
        }

    }
}
