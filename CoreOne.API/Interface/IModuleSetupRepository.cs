using CoreOne.DOMAIN.Models;
using System.Data;

namespace CoreOne.API.Interface
{
    public interface IModuleSetupRepository
    {

         DataTable GetMenuModule(int pageSize, int pageNumber, string? search, string? sortColumn, string? sortDir, string? searchCol);
        int SaveMenuWithModules(MenuWithModulesSave model);
        MenuModuleEditModel GetMenuModuleById(int menuId);
        void DeleteMenu(int menuId);
    }
}
