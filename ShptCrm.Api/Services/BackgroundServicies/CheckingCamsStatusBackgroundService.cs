namespace ShptCrm.Api.Services.BackgroundServicies
{
    public class CheckingCamsStatusBackgroundService: BackgroundService
    {
        private readonly ILogger<CheckingCamsStatusBackgroundService> _logger;
        private readonly CamStatusService _statusService;

        public CheckingCamsStatusBackgroundService(ILogger<CheckingCamsStatusBackgroundService> logger, CamStatusService statusService)
        {
            _logger = logger;
            _statusService = statusService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _statusService.CheckingStatus();
                }
                catch(Exception ex)
                {
                    _logger.LogError(nameof(CheckingCamsStatusBackgroundService), ex);
                }
                await Task.Delay(5_000);
            }
        }
    }
}
