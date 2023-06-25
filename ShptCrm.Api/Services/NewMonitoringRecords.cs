using Dapper;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System.Text.Json.Serialization;

namespace ShptCrm.Api.Services
{

    public class NewMonitoringRecords : IHostedService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NewMonitoringRecords> _logger;
        private System.Threading.Timer? _timer = null;
        private string dbConnectioName;
        private string dvrServer;
        public NewMonitoringRecords(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<NewMonitoringRecords> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            dbConnectioName = configuration.GetConnectionString("MySQL");
            dvrServer = configuration.GetConnectionString("DvrServer");
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, 0, 10_000);

            return Task.CompletedTask;
        }

        public async void DoWork(object? state)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient())
                using (MySqlConnection con = new MySqlConnection(dbConnectioName))
                {
                    con.Open();
                    var devRecording = await con.QueryAsync<ActVideo>("SELECT * FROM actshpt_video WHERE Status=0");
                    client.BaseAddress = new Uri(dvrServer);
                    foreach (ActVideo actVideo in devRecording)
                    {
                        var records = await client.GetFromJsonAsync<Record>($"/q.json?cmd=getevents&oid={actVideo.DevId}&ot=2");
                        var events = records?.events?.Where(r => r.sb > 0 & new DateTime(long.Parse( r.c)) >= DateTime.Now.AddDays(-1));
                        if (events == null || !events.Any())
                            continue;
                        var recordsInDb = await con.QueryAsync<string>($@"SELECT FileName FROM actshpt_files 
WHERE FileName IN ( {string.Join(",", events.Select(r => $"'{r.fn}'"))} )");
                        var newRows = from dvr in events
                                      join db in recordsInDb on dvr.fn equals db into gj
                                      from subpet in gj.DefaultIfEmpty()
                                      where subpet == null
                                      select $"({actVideo.ActId}, '{dvr.fn}', 0, {actVideo.DevId})";
                        if (newRows.Any())
                        {
                            await con.ExecuteAsync("INSERT INTO actshpt_files (ActId, FileName, Processed, DevId) VALUES " + string.Join(",", newRows));
                            await con.ExecuteAsync("UPDATE actshpt_video SET Status = 1 WHERE Stop IS NOT NULL AND id=" + actVideo.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка мониторинга записей\n" + ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();

        private class Record
        {
            public Event[] events { get; set; }
        }
        private class Event
        {
            public string c { get; set; }
            public int d { get; set; }
            public int dd { get; set; }
            public string fn { get; set; }
            public int sb { get; set; }
        }

        private class ActVideo
        {
            public int Id { get; set; }
            public int ActId { get; set; }
            public int DevId { get; set; }
        }
    }
}
