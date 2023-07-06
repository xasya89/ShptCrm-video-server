using System.Net.NetworkInformation;
using MySql.Data.MySqlClient;
using Dapper;
using System.Security.Cryptography.X509Certificates;

namespace ShptCrm.Api.Services.BackgroundServicies
{
    public class PingBackgroundService : BackgroundService
    {
        private readonly ILogger<PingBackgroundService> _logger;
        private readonly CamStatusService _statusService;
        private readonly ICamActionsService _actionsService;
        private string _connectionString;
        public PingBackgroundService(ILogger<PingBackgroundService> logger, 
            CamStatusService statusService,
            ICamActionsService actionsService)
        {
            _logger = logger;
            _statusService = statusService;
            _actionsService = actionsService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<int> disconnectedDevIds = new();
            var ping = new Ping();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var offlineCams = _statusService.GetStatus().Where(x => !x.IsOnline & x.ActId != null).ToList();

                    foreach (var cam in offlineCams)
                        if (disconnectedDevIds.Contains(cam.DevId))
                        {
                            await _actionsService.StopRecord(new List<int>() { cam.DevId });
                            disconnectedDevIds.Remove(cam.DevId);
                        }
                        else
                            disconnectedDevIds.Add(cam.DevId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(nameof(PingBackgroundService), ex);
                }


                await Task.Delay(15 * 60 * 1_000);
            }
        }
    }
}
