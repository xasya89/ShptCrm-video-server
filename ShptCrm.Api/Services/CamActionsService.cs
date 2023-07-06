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
        private readonly ICameraRecordControl _recordControl;
        private string _connectionString;
        public CamActionsService(IConfiguration configuration, 
            IHttpClientFactory clientFactory,
            ILogger<CamActionsService> logger,
            ICameraRecordControl recordControl)
        {
            _connectionString = configuration.GetConnectionString("MySQL");
            _recordControl = recordControl;
        }
        public async Task StartRecord(RecordStartModel model)
        {
            using(MySqlConnection con = new MySqlConnection(_connectionString))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    var camInRecrod = await con.QueryAsync<int>("SELECT DevId FROM actshpt_video WHERE Stop IS NULL AND DevId IN @Cams",
                        new { Cams = model.cams });
                    if (camInRecrod.Count() > 0)
                        throw new Exception($"Камеры {string.Join(',', model.cams)} уже записывают акт");
                    foreach (var cam in model.cams)
                    {
                        await con.ExecuteAsync($"INSERT INTO actshpt_video (ActId, DevId, Start, Status) VALUES ({model.actId}, {cam}, NOW(), 0)");
                        await _recordControl.StartRecord(cam);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    try
                    {
                        await _recordControl.StopRecord(model.cams.ToArray());
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
                        await _recordControl.StopRecord(devId);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error CamActionsService " + ex.Message);
                    transaction.Rollback();
                    try
                    {
                        await _recordControl.StartRecord(devsId.ToArray());
                    }
                    catch (Exception ) { };
                    throw ex;
                }
            }
        }

            
    }
}
