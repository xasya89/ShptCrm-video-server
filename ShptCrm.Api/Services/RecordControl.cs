using ShptCrm.Models.ConfigurationModels;
using System.Configuration;
using System.Net.Http;
using System.Net.NetworkInformation;

namespace ShptCrm.Api.Services
{
    public interface ICameraRecordControl
    {
        Task StartRecord(int devId);
        Task StartRecord(int[] devIds);
        Task StopRecord(int devId);
        Task StopRecord(int[] devIds);
        Task<IEnumerable<CamStatusModel>> GetStatus();
    }
    public class CameraRecordControl: ICameraRecordControl
    {
        private string _dvrServer;
        private readonly IHttpClientFactory _clientFactory;
        private readonly CamsSettings _camsSettings;

        public CameraRecordControl(IConfiguration configuration, IHttpClientFactory clientFactory, CamsSettings camsSettings)
        {
            _dvrServer = configuration.GetConnectionString("DvrServer");
            _clientFactory = clientFactory;
            _camsSettings = camsSettings;
        }

        public async Task StartRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            await client.GetAsync($"{_dvrServer}/command.cgi?cmd=record&ot=2&oid={devId}");
        }

        public async Task StartRecord(int[] devIds)
        {
            using var client = _clientFactory.CreateClient();
            foreach(var devId in devIds ) 
                await client.GetAsync($"{_dvrServer}/command.cgi?cmd=record&ot=2&oid={devId}");
        }

        public async Task StopRecord(int devId)
        {
            using var client = _clientFactory.CreateClient();
            await client.GetAsync($"{_dvrServer}/command.cgi?cmd=recordStop&ot=2&oid={devId}");
        }

        public async Task StopRecord(int[] devIds)
        {
            using var client = _clientFactory.CreateClient();
            foreach (var devId in devIds)
                await client.GetAsync($"{_dvrServer}/command.cgi?cmd=recordStop&ot=2&oid={devId}");
        }
        public async Task<IEnumerable<CamStatusModel>> GetStatus()
        {
            using var client = _clientFactory.CreateClient();
            using var ping = new Ping();
            var cams = _camsSettings.Settings.Select(x => new CamStatusModel { DevId = x.DevId, PathName = x.PathName });
            foreach(var cam in cams)
            {
                var status = await client.GetFromJsonAsync<CameraStatusModel>($"{_dvrServer}/command.cgi?cmd=getObject&ot=2&oid={cam.DevId}");
                cam.IsRecord = status.Data.Recording;
                var pingResult = await ping.SendPingAsync(_camsSettings.Settings.Where(x => x.DevId == cam.DevId).First().IpAdress);

                cam.IsOnline = pingResult.Status== IPStatus.Success;
            }
            return cams;
        }


        private class CameraStatusModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public CameraStatusData Data { get; set; }
        }

        private class CameraStatusData
        {
            public bool Online { get; set; }
            public bool Recording { get; set; }
        }
    }
}
