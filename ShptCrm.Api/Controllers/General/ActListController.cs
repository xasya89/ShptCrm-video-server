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
    //TODO: Создать UnitOfWOrk для репозиторий
    [Route("api/general/[controller]")]
    [ApiController]
    public class ActListController : ControllerBase
    {
        private string _connectionString;
        public ActListController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySQL");
        }

        [HttpGet]
        public async Task<IEnumerable<ActShpt>> Get()
        {
            using MySqlConnection con = new MySqlConnection(_connectionString);
            con.Open();
            return await ActShpRepository.GetList(con);
        }

        [HttpGet("{actId}")]
        public async Task<ActShpt> Get(int actId)
        {
            using MySqlConnection con = new MySqlConnection(_connectionString);
            con.Open();
            return await ActShpRepository.Get(con, actId);
        }

    }
}
