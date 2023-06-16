using System.Net.NetworkInformation;
using MySql.Data.MySqlClient;
using Dapper;

namespace ShptCrm.Api.Services.BackgroundServicies
{
    public class PingBackgroundService : BackgroundService
    {
        private readonly ILogger<PingBackgroundService> _logger;
        private readonly ICamActionsService _actionsService;
        private string _connectionString;
        private List<CamConfiguration> _camConfigurations = new();
        public PingBackgroundService(IConfiguration configuration, ILogger<PingBackgroundService> logger, ICamActionsService actionsService)
        {
            _logger = logger;
            configuration.GetSection("Cams").Bind(_camConfigurations);
            _connectionString = configuration.GetConnectionString("MySQL");
            _actionsService = actionsService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<int> disconnectedCams = new();
            var ping = new Ping();
            while(!stoppingToken.IsCancellationRequested)
            {
                if (_camConfigurations == null)
                    break;
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                    try
                    {
                        IEnumerable<int> devIdWrite = await con.QueryAsync<int>("SELECT DevId FROM actshpt_video WHERE Stop IS NULL");
                        List<int> stopedRecoredOnDevices = new List<int>();
                        foreach(int devId in devIdWrite)
                        {
                            string camIpAdress = _camConfigurations.Where(x => x.DevId == devId).First().IpAdress;
                            var resultPing = await ping.SendPingAsync(camIpAdress);

                            if (resultPing.Status != IPStatus.Success & disconnectedCams.Where(x => x == devId).Any())
                                stopedRecoredOnDevices.Add(devId);
                            if (resultPing.Status != IPStatus.Success & !disconnectedCams.Where(x => x == devId).Any())
                                disconnectedCams.Add(devId);
                            if(resultPing.Status == IPStatus.Success)
                                disconnectedCams.Remove(devId);
                        }
                        
                        if (stopedRecoredOnDevices.Count > 0)
                        {
                            await _actionsService.StopRecord(stopedRecoredOnDevices);
                            foreach (var devId in stopedRecoredOnDevices)
                                disconnectedCams.Remove(devId);
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError("Error PingBackgroundService: " + ex.Message);
                    }
                    await Task.Delay(1 * 60 * 1_000);
            }
        }
    }
}
