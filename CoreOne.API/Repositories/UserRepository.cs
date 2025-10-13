using CoreOne.API.Helpers;
using CoreOne.COMMON.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DBHelper _dbHelper;

        public UserRepository(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public int SaveUser(User model)
        {
            if (!string.IsNullOrEmpty(model.PasswordHash))
                model.PasswordHash = PasswordHelper.HashPassword(model.PasswordHash);

            var parameters = new Dictionary<string, object>
            {
                {"@UserID", model.UserID },
                {"@UserName", model.UserName },
                {"@PasswordHash", model.PasswordHash },
                {"@Email", model.Email },
                {"@MailTypeID", model.MailTypeID },
                {"@PhoneNumber", model.PhoneNumber },
                {"@RoleID", model.RoleID },
                {"@ActiveFlag", model.ActiveFlag },
                {"@CreatedBy", model.CreatedBy }
            };

            return _dbHelper.ExecuteSP_ReturnInt("sp_SaveUser", parameters);
        }

        public int AssignPermissions(List<UserPermissionModel> permissions)
        {
            int result = 0;
            foreach (var p in permissions)
            {
                var parameters = new Dictionary<string, object>
                {
                    {"@UserID", p.UserID},
                    {"@MenuModuleID", p.ModuleID},
                    {"@ActionID", p.ActionID},
                    {"@ActiveFlag", 1},
                    {"@CreatedBy", p.CreatedBy}
                };

                result += _dbHelper.ExecuteSP_ReturnInt("sp_SaveUserPermission", parameters);
            }
            return result;
        }

        

        public DataTable GetAllUsers()
        {
            var ds = _dbHelper.ExecuteSP_ReturnDataSet("sp_GetAllUsers", new Dictionary<string, object>());
            return ds.Tables[0];
        }

    }
}
