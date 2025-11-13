using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationsRepository _repo;

        public ApplicationsController(IApplicationsRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            var dt = _repo.GetAllApplications();

            var list = new List<ApplicationDto>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ApplicationDto
                {
                    ApplicationID = Convert.ToInt32(row["ApplicationID"]),
                    ApplicationName = row["ApplicationName"].ToString()
                });
            }

            return Ok(list);
        }

    }
}
