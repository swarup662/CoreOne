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





        [HttpPost("GetActionById")]
        public IActionResult GetActionById([FromBody] int actionId)
        {
            if (actionId <= 0) return BadRequest("Invalid ActionId.");

            var action = _actionRepo.GetActionById(actionId);

            if (action == null)
                return NotFound(new { Message = "Action not found." });

            return Ok(action);
        }



        [HttpPost("AddAction")]
        public IActionResult AddAction([FromBody] ActionCreationDto act)
        {
            if (act == null || string.IsNullOrWhiteSpace(act.ActionName))
                return BadRequest("Action name is required.");

            int newId = _actionRepo.SaveAction("INSERT", null, act.ActionName, act.Description, act.CreatedBy ?? 0);

            return Ok(new { ActionID = newId, Message = "Action created successfully." });
        }

        [HttpPost("UpdateAction")]
        public IActionResult UpdateAction([FromBody] ActionCreationDto act)
        {
            if (act == null || act.ActionID <= 0)
                return BadRequest("Valid Action is required.");

            int result = _actionRepo.SaveAction("UPDATE", act.ActionID, act.ActionName, act.Description, act.CreatedBy ?? 0);

            if (result > 0)
                return Ok(new { ActionID = result, Message = "Action updated successfully." });

            return NotFound(new { ActionID = result, Message = "Action not found or not updated." });
        }


      


    }




}
