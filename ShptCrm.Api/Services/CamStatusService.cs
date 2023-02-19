using MySqlX.XDevAPI;

namespace ShptCrm.Api.Services
{
    public class CamStatusService
    {
        private string dvrServer;
        private readonly IEnumerable<CamStatusModel> cams;
        private readonly IHttpClientFactory _httpClient;
        public CamStatusService(IConfiguration configuration, IHttpClientFactory httpClient)
        {
            cams = configuration.GetSection("Cams").Get(typeof(IEnumerable<CamStatusModel>)) as IEnumerable<CamStatusModel>;
            dvrServer = configuration.GetConnectionString("DvrServer");
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
            return cams;
        }

        public void StartRecord(int devId, int actId)
        {
            var cam = cams.Where(c => c.DevId == devId).First();
            if (cam.ActId != null & cam.ActId!=actId)
                throw new ArgumentNullException($"Камера - {cam.DevId} уже записывает другой акт");
            cam.ActId = actId;

        }
    }
}
