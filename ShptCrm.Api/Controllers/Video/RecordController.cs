using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShptCrm.Api.Services;
using Dapper;
using MySql.Data.MySqlClient;
using System;

namespace ShptCrm.Api.Controllers.Video
{
    [Route("api/video/[controller]")]
    [ApiController]
    public class RecordController : ControllerBase
    {
        private readonly ICamActionsService _actionsService;
        public RecordController(ICamActionsService actionsService)
        {
            _actionsService = actionsService;
        }

        [HttpPost]
        public async Task<IActionResult> Post( [FromBody] RecordPostModel model)
        {
            await _actionsService.StartRecord(model);
                
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] IEnumerable<int> devsId)
        {
            _actionsService.StopRecord(devsId);

            return Ok();
        }

        public class RecordPostModel
        {
            public int ActId { get; set; }
            public IEnumerable<int> Cams { get; set; }
        }
    }
}
