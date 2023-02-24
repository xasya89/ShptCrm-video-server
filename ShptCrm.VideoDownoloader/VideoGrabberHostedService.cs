using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MySqlX.XDevAPI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ShptCrm.VideoDownoloader
{
    internal class VideoGrabberHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VideoGrabberHostedService> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private string pathVideo;
        private string conStr;
        private string videoServerUri;

        public VideoGrabberHostedService(IConfiguration configuration, ILogger<VideoGrabberHostedService> logger, IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _clientFactory = clientFactory;
            pathVideo = configuration.GetSection("PathOut").Value.ToString();
            conStr = configuration.GetConnectionString("MySQL");
            videoServerUri = configuration.GetConnectionString("VideoServer");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using HttpClient client = _clientFactory.CreateClient();
                client.BaseAddress = new Uri(videoServerUri);
                using MySqlConnection con = new MySqlConnection(conStr);
                con.Open();
                var actFiles = await con.QueryAsync<ActFile>($"SELECT * FROM actshpt_files WHERE Processed={(int)ProcessedStatus.New} OR Processed={(int)ProcessedStatus.Retry}");
                foreach (var actFile in actFiles)
                    try
                    {
                        if(await findVideo(client, actFile))
                        {
                            await grabMkv(client, actFile);
                            await grabImage(client, actFile);
                            await con.ExecuteAsync($"UPDATE actshpt_files SET Processed={(int)ProcessedStatus.Complite} WHERE id=" + actFile.Id);
                        }
                        else
                            await con.ExecuteAsync($"UPDATE actshpt_files SET Processed={(int)ProcessedStatus.Error} WHERE id=" + actFile.Id);
                    }
                    catch(SystemException ex) 
                    {
                        _logger.LogError(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    };
                await Task.Delay(60_000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }

        private async Task grabMkv(HttpClient httpClient, ActFile actFile)
        {
            using var resp = await httpClient.GetAsync($"streamfile?oid={actFile.DevId}&ot=2&fn={actFile.FileName}");
            resp.EnsureSuccessStatusCode();
            var stream = resp.Content.ReadAsStreamAsync();
            string pathOut = Path.Combine(pathVideo, actFile.ActId.ToString());
            if (!Directory.Exists(pathOut))
                Directory.CreateDirectory(pathOut);
            string fileName = Path.Combine(pathOut, actFile.FileName);
            if (File.Exists(fileName))
                File.Delete(fileName);
            using FileStream fs = new FileStream(fileName, FileMode.CreateNew);
            await resp.Content.CopyToAsync(fs);
        }



        private async Task grabImage(HttpClient httpClient, ActFile actFile)
        {
            using var resp = await httpClient.GetAsync($"filethumb.jpg?oid={actFile.DevId}&fn={actFile.FileName.Replace(".mkv", "_large.jpg")}");
            resp.EnsureSuccessStatusCode();
            var stream = resp.Content.ReadAsStreamAsync();
            string fileName = Path.Combine(pathVideo, actFile.ActId.ToString(), actFile.FileName.Replace(".mkv",".jpg"));
            if (File.Exists(fileName))
                File.Delete(fileName);
            using FileStream fs = new FileStream(fileName, FileMode.CreateNew);
            await resp.Content.CopyToAsync(fs);
        }

        async Task<bool> findVideo(HttpClient httpClient, ActFile actFile)
        {
            var eventList = await httpClient.GetFromJsonAsync<EventList>($"q.json?cmd=getevents&oid={actFile.DevId}&ot=2");
            return eventList.Events.Any(x => x.fn == actFile.FileName);
        }

        class EventList
        {
            public IEnumerable<Event> Events { get; set; }
        }
        class Event
        {
            public string fn { get; set; }
        }

        class ActFile
        {
            public int Id { get; set; }
            public int ActId { get; set; }
            public string FileName { get; set; }
            public int DevId { get; set; }

        }

        enum ProcessedStatus
        {
            New = 0,
            Complite = 1,
            Error = 2,
            Retry = 3
        }
    }
}
