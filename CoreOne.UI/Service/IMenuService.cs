using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace CoreOne.UI.Service
{
    public interface IMenuService
    {
        Task<List<MenuItem>> GetUserMenu(HttpContext context);
    }
}