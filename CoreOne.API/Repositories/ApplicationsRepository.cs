using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Repositories
{
    public class ApplicationsRepository : IApplicationsRepository
    {
        private readonly DBContext _dbHelper;
        public ApplicationsRepository(DBContext dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllApplications()
        {
            return _dbHelper.ExecuteSP_ReturnDataTable("sp_GetAllApplications");
        }
    }

}
