using CoreOne.DOMAIN.Models;
using System.Data;

namespace CoreOne.API.Interfaces
{
    public interface IUserCreationRepository
    {
        DataTable GetUsers(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol, string? status);
        DataTable? GetRoles(int userId);
        DataTable? GetGenders(int userId);
        DataTable? GetMailtypes(int userId);
        int SaveUser(string recType, UserCreation user);

        int UpdateUser(string recType, UserCreationEditDTO user);
        UserCreation? GetUserById(int userId);
        Task<int> SaveExtraPermissionAsync(int CreatedBy ,IEnumerable<ExtraPermission> permissions);
        Task<IEnumerable<ExtraPermission>> GetExtraPermissionByUserId(int UserId, int CreatedBy);
        int ActivateDeactivateUser(string recType, UserCreationDTO user);




    }
}
