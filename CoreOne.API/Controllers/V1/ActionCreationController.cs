using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.DOMAIN.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CoreOne.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ActionCreationController : ControllerBase
    {
        private readonly IActionCreationRepository _actionRepo;

        public ActionCreationController(IActionCreationRepository actionRepo)
        {
            _actionRepo = actionRepo;
        }

        [HttpGet("GetActions")]
        public async Task<IActionResult> GetActions()
        {
            var data = await _actionRepo.GetActions();
            return Ok(data);
        }

    }
}
