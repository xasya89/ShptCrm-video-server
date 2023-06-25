using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Dapper;

namespace ShptCrm.Api.Services
{
    public class CamStatusService
    {
        private string connectionStr;
        private readonly ICameraRecordControl _recordControll;
        public CamStatusService(IConfiguration configuration, ICameraRecordControl recordControll)
        {
            connectionStr = configuration.GetConnectionString("MySQL");
            _recordControll = recordControll;
        }

        public async Task<IEnumerable<CamStatusModel>> GetStatus()
        {
            var cams = await _recordControll.GetStatus();
            
            using MySqlConnection con = new MySqlConnection(connectionStr);
            con.Open();
            var recordDevIdList = await con.QueryAsync<CamStatusModel>("SELECT v.ActId, a.ActNum, a.ActDate, a.Fahrer AS ActFahrer, a.CarNum AS ActCar, v.DevId FROM actshpt a, actshpt_video v WHERE a.id=v.actId AND v.Stop IS NULL ");
            recordDevIdList = recordDevIdList.GroupBy(x => x.ActId).Select(x => x.First());
            return from cam in cams
                   join act in recordDevIdList on cam.DevId equals act.DevId into t
                   from sub in t.DefaultIfEmpty()
                   select new CamStatusModel
                   {
                       DevId = cam.DevId,
                       PathName = cam.PathName,
                       ActId = sub?.ActId,
                       ActNum = sub?.ActNum,
                       ActDate = sub?.ActDate,
                       IsRecord = cam.IsRecord,
                       IsOnline = cam.IsOnline
                   };
        }
    }
}
