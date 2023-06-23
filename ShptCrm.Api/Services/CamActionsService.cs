using Dapper;
using MySql.Data.MySqlClient;
using ShptCrm.Models.RecordModel;
using System.Security.Policy;
using System.Text.Json;
using static ShptCrm.Api.Controllers.Video.RecordController;

namespace ShptCrm.Api.Services
{
    public interface ICamActionsService
    {
        public Task StartRecord(RecordStartModel model);
        public Task StopRecord(IEnumerable<int> devsId);
    }
    public class CamActionsService : ICamActionsService
    {
        private readonly ILogger<CamActionsService> _logger;
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
        public async Task StartRecord(RecordStartModel model)
        {
            using(MySqlConnection con = new MySqlConnection(_connectionString))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    var camInRecrod = await con.QueryAsync<int>("SELECT DevId FROM actshpt_video WHERE Stop IS NULL AND DevId IN @Cmas",
                        new { Cams = model.cams });
                    if (camInRecrod.Count() > 0)
                        throw new Exception($"Камеры {string.Join(',', model.cams)} уже записывают акт");
                    foreach (var cam in model.cams)
                    {
                        await con.ExecuteAsync($"INSERT INTO actshpt_video (ActId, DevId, Start, Status) VALUES ({model.actId}, {cam}, NOW(), 0)");
                        await sendStartRecord(cam);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    //_logger.LogError("Error CamActionsService " + ex.Message + "\n" + JsonSerializer.Serialize(model) + "\n" + _dvrServer);
                    transaction.Rollback();
                    try
                    {
                        foreach (var cam in model.cams)
                            await sendStopRecord(cam);
                    }
                    catch (Exception ) { }
                    throw new Exception( JsonSerializer.Serialize(model) + "\n" + _dvrServer);
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

        private async Task sendStartRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            await client.GetAsync($"{_dvrServer}/command.cgi?cmd=record&ot=2&oid={devId}");
        }

        private async Task sendStopRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            await client.GetAsync($"{_dvrServer}/command.cgi?cmd=recordStop&ot=2&oid={devId}");
        }
            
    }
}
