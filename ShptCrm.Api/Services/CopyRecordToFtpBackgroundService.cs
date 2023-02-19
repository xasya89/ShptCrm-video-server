using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Net;
using System.IO;
using CliWrap;
using System.Runtime.CompilerServices;

namespace ShptCrm.Api.Services
{
    /// <summary>
    /// Сервис для копирования записей в ftp. Записи в бд со статусом processed=0
    /// </summary>
    public class RecordProcessingBackgroundService : BackgroundService
    {
        private readonly IEnumerable<CamStatusModel> cams;
        private readonly IServiceProvider _provider;
        private string ftpFolderPath;
        private string ffmpegPath;
        public RecordProcessingBackgroundService(IConfiguration configuration, IServiceProvider provider)
        {
            ftpFolderPath = configuration.GetConnectionString("FtpFolderPath");
            ffmpegPath = configuration.GetConnectionString("FFMpeg");
            if(!Directory.Exists(ftpFolderPath))
                Directory.CreateDirectory(ftpFolderPath);
            cams = configuration.GetSection("Cams").Get(typeof(IEnumerable<CamStatusModel>)) as IEnumerable<CamStatusModel>;
            _provider = provider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _provider.CreateScope();
            var conn = scope.ServiceProvider.GetRequiredService<MySQLConnectionService>().GetConnection();


            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    foreach (var actFile in await conn.QueryAsync<ActFileModel>("SELECT * FROM actshpt_files WHERE Processed=0"))
                    {
                        int actNum = await conn.QuerySingleAsync<int>("SELECT * FROM actshpt WHERE id=" + actFile.ActId);
                        string pathout = Path.Combine(ftpFolderPath, actNum.ToString());
                        if (!Directory.Exists(pathout))
                            Directory.CreateDirectory(pathout);
                        string picturePath = Path.Combine(cams.First(c => c.DevId == actFile.DevId).PathName, "thumbs");
                        string fileIn = Path.Combine(cams.First(c => c.DevId == actFile.DevId).PathName, actFile.FileName);
                        string fileOut = Path.Combine(pathout, actFile.FileName);
                        if (File.Exists(fileIn) & !File.Exists(fileOut))
                        {
                            File.Copy(fileIn, fileOut);
                            string fileOutJpg = fileOut.Replace(".mkv", "_large.jpg");
                            if (fileOut.IndexOf("mp4") > -1)
                                fileOutJpg = fileOut.Replace(".mp4", "_large.jpg");
                            if (File.Exists(Path.Combine(picturePath, fileOutJpg)))
                                File.Copy(
                                    Path.Combine(picturePath, fileOutJpg),
                                    Path.Combine(ftpFolderPath, fileOutJpg.Replace("_large", ""))
                                    );
                            else
                                try
                                {
                                    fileOutJpg = fileOutJpg.Replace("_large", "");
                                    await Cli.Wrap(ffmpegPath)
                    .WithArguments($"-i \"{fileOut}\" -ss 00:00:05 -vf scale=640:480 -vframes 1 \"{fileOutJpg}\"")
                    .WithWorkingDirectory(ftpFolderPath)
                    .ExecuteAsync();
                                }
                                catch (Exception ex) { }
                        }
                        await conn.ExecuteAsync("UPDATE actshpt_files SET Processed=1 WHERE id=" + actFile.Id);
                    }
                    await Task.Delay(60 * 1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                }
        }

        private class ActFileModel
        {
            public int Id { get; set; }
            public int ActId { get; set; }
            public string FileName { get; set; }
            public bool Processed { get; set; }
            public int DevId { get; set; }
        }
    }

    /// <summary>
    /// Сервис сканирования каталога на предмет новых записей и добавления в бд со статусом processed=0
    /// </summary>
    public class MonitorNewRecordsBackgroundService : BackgroundService
    {
        private readonly IEnumerable<CamStatusModel> cams;
        //private readonly MySqlConnection _conn;
        private readonly Dictionary<int, bool> camRocordStarting;
        private readonly Dictionary<int, List<string>> fileAppends;
        private readonly IServiceProvider _serviceProvider;

        public MonitorNewRecordsBackgroundService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            cams = configuration.GetSection("Cams").Get(typeof(IEnumerable<CamStatusModel>)) as IEnumerable<CamStatusModel>;
            camRocordStarting = new Dictionary<int, bool>(cams.Select(cam => new KeyValuePair<int, bool>(cam.DevId, false)));
            fileAppends = new(cams.Select(c => new KeyValuePair<int, List<string>>(c.DevId, new())));
            InitService();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var conn = scope.ServiceProvider.GetRequiredService<MySQLConnectionService>().GetConnection();

            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    MonitoringRecordTask task = null;
                    MonitoringRecordQueue.TryDequeue(out task);
                    if (task != null && task.IsRunning)
                        camRocordStarting[task.DevId] = true;
                    foreach (int devId in camRocordStarting.Where(c => c.Value == true).Select(c => c.Key))
                        AnalyzeNewRocrods(conn, devId);
                    if (task != null && !task.IsRunning)
                    {
                        camRocordStarting[task.DevId] = false;
                        AnalyzeNewRocrods(conn, task.DevId);
                        await conn.ExecuteAsync($"UPDATE actshpt_video SET Stop=NOW() WHERE Stop IS NULL AND DevId={task.DevId}");
                    }
                    await Task.Delay(60 * 1000, stoppingToken);
                }
                catch (OperationCanceledException ex) { }
        }

        /// <summary>
        /// При старте проверим есть ли пропущенные файлы в случае падения сервиса
        /// </summary>
        private void InitService()
        {
            using var scope = _serviceProvider.CreateScope();
            var conn = scope.ServiceProvider.GetRequiredService<MySQLConnectionService>().GetConnection();
            foreach (var cam in cams)
                if (conn.QuerySingle<int>("SELECT COUNT(*) FROM actshpt_video WHERE Stop IS NULL AND DevId=" + cam.DevId) == 1)
                {
                    MonitoringRecordQueue.Enqueue(new MonitoringRecordTask { DevId = cam.DevId, IsRunning = true });
                    int devId = cam.DevId;
                    DirectoryInfo dir = new DirectoryInfo(cam.PathName);
                    var appends = fileAppends[devId];
                    int actId = conn.Query<int>("SELECT ActId FROM actshpt_video WHERE Stop IS NULL AND DevId=" + devId).First();
                    DateTime actStart = conn.Query<DateTime>("SELECT Start FROM actshpt_video WHERE Stop IS NULL AND DevId=" + devId).First();
                    IEnumerable<FileInfo> filesNotDb = getFielListNotDb(conn,
                        dir.GetFiles().Where(f => compareFileDate(f.CreationTime))
                        );
                    foreach (FileInfo f in filesNotDb)
                        if (f.CreationTime >= actStart.AddMinutes(-1) && appends.Where(a => a == f.Name).FirstOrDefault() == null)
                            try
                            {
                                using (FileStream fr = f.OpenWrite())
                                    appends.Add(f.Name);
                                if (conn.QuerySingle<int>($"SELECT COUNT(*) FROM actshpt_files WHERE FileName='{f.Name}'") == 0)
                                    conn.Execute($"INSERT INTO actshpt_files (ActId, DevId, FileName, Processed) VALUES ({actId}, {devId}, '{f.Name}', 0)");
                            }
                            catch (IOException) { };
                }
        }

        //Ищем новые файлы и добавляем из в бд для обработки
        private void AnalyzeNewRocrods(MySqlConnection conn, int devId)
        {
            var cam = cams.Where(c => c.DevId == devId).FirstOrDefault();
            DirectoryInfo dir = new DirectoryInfo(cam.PathName);
            var appends = fileAppends[devId];
            int actId = conn.Query<int>("SELECT ActId FROM actshpt_video WHERE Stop IS NULL AND DevId=" + devId).First();
            DateTime actStart = conn.Query<DateTime>("SELECT Start FROM actshpt_video WHERE Stop IS NULL AND DevId=" + devId).First();
            IEnumerable<FileInfo> filesNotDb = getFielListNotDb(conn,
                dir.GetFiles().Where(f => compareFileDate(f.CreationTime)));
            foreach (FileInfo f in filesNotDb)
                if (f.CreationTime >= actStart.AddMinutes(-1) && appends.Where(a => a == f.Name).FirstOrDefault() == null)
                    try
                    {
                        using (FileStream fr = f.OpenWrite())
                            appends.Add(f.Name);
                        conn.Execute($"INSERT INTO actshpt_files (ActId, DevId, FileName, Processed) VALUES ({actId}, {devId}, '{f.Name}', 0)");
                    }
                    catch (IOException) { };
        }

        //Используется, чтобы отсечь файлы, которые созданы более чем вчера
        private bool compareFileDate(DateTime createDateTime) =>
            DateTime.Compare(DateTime.Now.AddDays(-1), createDateTime) < 0;

        /// <summary>
        /// Проверяет наличие файлов в бд
        /// </summary>
        /// <param name="con"></param>
        /// <param name="fList"></param>
        /// <returns>Список файлов, которые не добавлены в бд</returns>
        private IEnumerable<FileInfo> getFielListNotDb(MySqlConnection con, IEnumerable<FileInfo> fList)
        {
            var namesInDb = con.Query<string>("SELECT FileName FROM actshpt_files WHERE FileName IN @FileNames",
                new { FileNames = fList.Select(f=>f.Name).ToList() });
            return fList.Where(f=>!namesInDb.Contains(f.Name));
        }

        public static ConcurrentQueue<MonitoringRecordTask> MonitoringRecordQueue = new ConcurrentQueue<MonitoringRecordTask>();

        public static void StartRecord(int devId) => MonitoringRecordQueue.Enqueue(new MonitoringRecordTask { DevId = devId, IsRunning = true });
        public static void StopRecord(int devId) => MonitoringRecordQueue.Enqueue(new MonitoringRecordTask { DevId = devId, IsRunning = false });
        public class MonitoringRecordTask
        {
            public int DevId { get; set; }
            public bool IsRunning { get; set; }
        }
    }
}
