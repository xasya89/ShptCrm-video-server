using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShptCrm.Api.Services;
using Dapper;
using MySql.Data.MySqlClient;
using System;
using ShptCrm.Models.RecordModel;

namespace ShptCrm.Api.Controllers.Video
{
    [Route("api/video/[controller]")]
    public class RecordController : BaseApiController
    {
        private readonly ICamActionsService _actionsService;
        public RecordController(ICamActionsService actionsService)
        {
            _actionsService = actionsService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Post( [FromBody] RecordStartModel model)
        {
            try
            {
                await _actionsService.StartRecord(model);
            }
            catch(Exception ex)
            {
                return await ExceptionResult(ex);
            }
            return Ok();
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Put([FromBody] IEnumerable<int> devsId)
        {
            try
            {
                await _actionsService.StopRecord(devsId);
            }
            catch(Exception ex)
            {
                return await ExceptionResult(ex);
            }
            return Ok();
        }
    }
}
