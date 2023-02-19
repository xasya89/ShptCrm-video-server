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
        private readonly CamStatusService _camStatus;
        private readonly MySqlConnection _conn;
        private readonly IHttpClientFactory _clientFactory;
        private string dvrServer;
        public RecordController(IConfiguration configuration, CamStatusService camStatus, MySQLConnectionService connService, IHttpClientFactory clientFactory)
        {
            _camStatus= camStatus;
            _conn = connService.GetConnection();
            _clientFactory = clientFactory;
            dvrServer = configuration.GetConnectionString("DvrServer");
        }

        [HttpPost]
        public async Task<IActionResult> Post( [FromBody] RecordPostModel model)
        {
            using var client = _clientFactory.CreateClient();
            foreach (var cam in model.Cams)
            {
                await _conn.ExecuteAsync($"INSERT INTO actshpt_video (ActId, DevId, Start) VALUES ({model.ActId}, {cam}, NOW())");
                await client.GetAsync($"{dvrServer}/command.cgi?cmd=record&ot=2&oid={cam}");
                MonitorNewRecordsBackgroundService.StartRecord(cam);
            }
                
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] IEnumerable<int> cams)
        {
            using var client = _clientFactory.CreateClient();
            foreach (var cam in cams)
            {
                await client.GetAsync($"{dvrServer}/command.cgi?cmd=recordStop&ot=2&oid={cam}");
                MonitorNewRecordsBackgroundService.StopRecord(cam);
            }

            return Ok();
        }

        public class RecordPostModel
        {
            public int ActId { get; set; }
            public IEnumerable<int> Cams { get; set; }
        }
    }
}
