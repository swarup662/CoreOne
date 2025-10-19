using CoreOne.DOMAIN.Models;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public interface IUserRepository
    {
        int SaveUser(User model);
        int AssignPermissions(List<UserPermission> permissions);
        DataTable GetAllUsers();
    }
}
