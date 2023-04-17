using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Dapper;

namespace ShptCrm.Api.Services
{
    public class CamStatusService
    {
        private string dvrServer;
        private List<CamStatusModel> cams;
        private readonly IHttpClientFactory _httpClient;
        private string connectionStr;
        public CamStatusService(IConfiguration configuration, IHttpClientFactory httpClient)
        {
            cams = configuration.GetSection("Cams").Get(typeof(List<CamStatusModel>)) as List<CamStatusModel>;
            dvrServer = configuration.GetConnectionString("DvrServer");
            connectionStr = configuration.GetConnectionString("MySQL");
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CamStatusModel>> GetStatus()
        {
            foreach (var cam in cams)
                using (var httpClient = _httpClient.CreateClient())
                {
                    var status = await httpClient.GetFromJsonAsync<CameraStatusModel>($"{dvrServer}/command.cgi?cmd=getObject&ot=2&oid={cam.DevId}");
                    cam.IsRecord = status.Data.Recording;
                    cam.IsOnline = status.Data.Online;
                }
            
            using MySqlConnection con = new MySqlConnection(connectionStr);
            con.Open();
            var recordDevIdList = await con.QueryAsync<CamStatusModel>("SELECT v.ActId, a.ActNum, a.ActDate, a.Fahrer AS ActFahrer, a.CarNum AS ActCar, v.DevId FROM actshpt a, actshpt_video v WHERE a.id=v.actId AND v.Stop IS NULL GROUP BY a.id");
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
