using Microsoft.AspNetCore.Mvc;
using ShptCrm.Api.Controllers;
using System.IO;
using MySql.Data.MySqlClient;
using Dapper;
using System;
using System.Diagnostics;

namespace ShptCrm.Api.Services
{
    public class PhotoUploadService
    {
        private string connectionStr;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public PhotoUploadService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            connectionStr = configuration.GetConnectionString("MySQL");
        }

        public FileStream Get(string fileName) =>
            File.OpenRead(Path.Combine(_env.ContentRootPath, "uploads", fileName));

        public async Task<IEnumerable<string>> Upload(int actId, IFormFileCollection files)
        {
            var result = new List<String>();
            string basePath = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            using(MySqlConnection con= new MySqlConnection(connectionStr))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                foreach (var f in files)
                    using (var fs = new FileStream(Path.Combine(basePath, f.FileName), FileMode.OpenOrCreate))
                    {
                        result.Add(f.FileName);
                        await f.CopyToAsync(fs);
                        await con.ExecuteAsync("INSERT INTO actshpt_files (ActId, FileName, Processed, DevId) VALUES (@ActId, @FileName, @Processed, @DevId)",
                            new { ActId = actId, FileName = f.FileName, Processed = 1, DevId = (int?)null });
                    }
                transaction.Commit();
            }
            return result;
        }

        public async Task Upload(ImageUploadModel model)
        {
            byte[] upload = Convert.FromBase64String(model.File.Replace("data:image/jpeg;base64,", ""));
            if (upload?.Length == 0) throw new Exception("Файл пустой");
            string path = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "img.jpg");
            File.WriteAllBytesAsync(path, upload);
        }

        public async Task Upload( IFormFile fileUpload)
        {
            if (fileUpload == null) throw new Exception("Файл пустой");
            string path = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, fileUpload.FileName);
            using (FileStream fs = new FileStream(path, FileMode.Create))
                await fileUpload.CopyToAsync(fs);

        }
    }
}
