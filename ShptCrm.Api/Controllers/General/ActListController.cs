using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using ShptCrm.Api.Services;
using System.Collections;
using ShptCrm.Models;
using Dapper;

namespace ShptCrm.Api.Controllers.General
{
    [Route("api/general/[controller]")]
    [ApiController]
    public class ActListController : ControllerBase
    {
        private readonly MySQLConnectionService _conn;
        public ActListController(MySQLConnectionService conn)
        {
            _conn = conn;
        }

        [HttpGet]
        public async Task<IEnumerable<ActShpt>> Get() => await ActShpRepository.GetList(_conn.GetConnection());

        [HttpGet("{actId}")]
        public async Task<ActShpt> Get(int actId) => await ActShpRepository.Get(_conn.GetConnection(), actId);

    }
}
