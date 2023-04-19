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
using System.Data;

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
        private string crmServerUri;

        public VideoGrabberHostedService(IConfiguration configuration, ILogger<VideoGrabberHostedService> logger, IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _clientFactory = clientFactory;
            pathVideo = configuration.GetSection("PathOut").Value.ToString();
            conStr = configuration.GetConnectionString("MySQL");
            videoServerUri = configuration.GetConnectionString("VideoServer");
            crmServerUri = configuration.GetConnectionString("ShptCrmServer");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using HttpClient client = _clientFactory.CreateClient();
                using HttpClient clientCrmServer = _clientFactory.CreateClient();
                client.BaseAddress = new Uri(videoServerUri);
                clientCrmServer.BaseAddress = new Uri(crmServerUri);

                using MySqlConnection con = new MySqlConnection(conStr);
                con.Open();
                /*
                var actFiles = await con.QueryAsync<ActFile>($@"SELECT f.* FROM actshpt_video v, actshpt_files f 
WHERE v.actId=f.actId AND v.stop IS NOT NULL AND (f.Processed={(int)ProcessedStatus.New} OR f.Processed={(int)ProcessedStatus.Retry})");
                */
                string dateOrig = "2023-02-19_16-57-26";
                string[] pair = dateOrig.Split('_');
                dateOrig = pair[0] + "T" + pair[1].Replace("-", ":");
                var tt = DateTime.Parse(dateOrig);

                var actFiles = await con.QueryAsync<ActFile>($@"SELECT * FROM actshpt_files 
WHERE Processed={(int)ProcessedStatus.New} OR Processed={(int)ProcessedStatus.Retry}");

                foreach (var actFile in actFiles.Where(a => a.DevId == null))
                    try
                    {
                        await getPhoto(clientCrmServer, actFile);
                        await con.ExecuteAsync($"UPDATE actshpt_files SET Processed={(int)ProcessedStatus.Complite} WHERE id=" + actFile.Id);
                    }
                    catch (Exception e)
                    {
                        await con.ExecuteAsync($"UPDATE actshpt_files SET Processed={(int)ProcessedStatus.Error} WHERE id=" + actFile.Id);
                    }

                actFiles = actFiles.Where(a=>a.DevId!=null).Where(f =>
                {
                    string dateOrig = f.FileName.Substring(f.DevId.ToString().Length + 1).Replace(".mkv", "");
                    string[] pair = dateOrig.Split('_');
                    dateOrig = pair[0] + "T" + pair[1].Replace("-", ":");
                    return DateTime.Parse(dateOrig) < DateTime.Now.AddMinutes(-10);
                });
                foreach (var actFile in actFiles.Where(a=>a.DevId!=null))
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
                await Task.Delay(300_000);//5 мин
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
            var stream = await resp.Content.ReadAsStreamAsync();
            string pathOut = Path.Combine(pathVideo, actFile.ActId.ToString());
            if (!Directory.Exists(pathOut))
                Directory.CreateDirectory(pathOut);
            string fileName = Path.Combine(pathOut, actFile.FileName);
            if (File.Exists(fileName))
                File.Delete(fileName);
            Console.WriteLine("Stream - " + actFile.FileName + " - "+stream.Length.ToString());
            using FileStream fs = new FileStream(fileName, FileMode.CreateNew);
            await stream.CopyToAsync(fs);
            //await resp.Content.CopyToAsync(fs);
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

        private async Task getPhoto(HttpClient client, ActFile actFile)
        {
            using var stream = await client.GetStreamAsync("/api/PhotoUpload/" + actFile.FileName);
            if (!Directory.Exists(Path.Combine(pathVideo, actFile.ActId.ToString())))
                Directory.CreateDirectory(Path.Combine(pathVideo, actFile.ActId.ToString()));
            string fileName = Path.Combine(pathVideo, actFile.ActId.ToString(), actFile.FileName);
            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate))
                await stream.CopyToAsync(fs);

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
            public int? DevId { get; set; }

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
