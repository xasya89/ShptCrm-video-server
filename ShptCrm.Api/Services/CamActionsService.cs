using Dapper;
using MySql.Data.MySqlClient;
using System.Security.Policy;
using static ShptCrm.Api.Controllers.Video.RecordController;

namespace ShptCrm.Api.Services
{
    public interface ICamActionsService
    {
        public Task StartRecord(RecordPostModel model);
        public Task StopRecord(IEnumerable<int> devsId);
    }
    public class CamActionsService : ICamActionsService
    {
        private readonly ILogger<CamActionsService> _logger;
        private readonly MySqlConnection _conn;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        private string _dvrServer;
        private string _connectionString;
        public CamActionsService(IConfiguration configuration, 
            IHttpClientFactory clientFactory,
            ILogger<CamActionsService> logger)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
            _dvrServer = configuration.GetConnectionString("DvrServer");
            _connectionString = configuration.GetConnectionString("MySQL");
        }
        public async Task StartRecord(RecordPostModel model)
        {
            using(MySqlConnection con = new MySqlConnection(_connectionString))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    foreach (var cam in model.Cams)
                    {
                        await _conn.ExecuteAsync($"INSERT INTO actshpt_video (ActId, DevId, Start, Status) VALUES ({model.ActId}, {cam}, NOW(), 0)");
                        await sendStartRecord(cam);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error CamActionsService " + ex.Message);
                    transaction.Rollback();
                    try
                    {
                        foreach (var cam in model.Cams)
                            await sendStopRecord(cam);
                    }
                    catch (Exception ) { }
                    throw ex;
                }
            }
        }


        public async Task StopRecord(IEnumerable<int> devsId)
        {
            using (MySqlConnection con = new MySqlConnection(_connectionString))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    foreach (var devId in devsId)
                    {
                        await con.ExecuteAsync($"UPDATE actshpt_video SET Stop=NOW() WHERE devId=" + devId);
                        await sendStopRecord(devId);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error CamActionsService " + ex.Message);
                    transaction.Rollback();
                    try
                    {
                        foreach (var devId in devsId)
                            await sendStartRecord(devId);
                    }
                    catch (Exception ) { };
                    throw ex;
                }
            }
        }

        private Task sendStartRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            return client.GetAsync($"{_dvrServer}/command.cgi?cmd=record&ot=2&oid={devId}");
        }

        private Task sendStopRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            return client.GetAsync($"{_dvrServer}/command.cgi?cmd=recordStop&ot=2&oid={devId}");
        }
            
    }
}
