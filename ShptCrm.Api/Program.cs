using ShptCrm.Api.Services;
using ShptCrm.Api.Services.BackgroundServicies;
using System.Diagnostics;

namespace ShptCrm.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            if (isService && OperatingSystem.IsWindows())
                builder.Host.UseWindowsService();
            if (isService && OperatingSystem.IsLinux())
                builder.Host.UseSystemd();
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  policy =>
                                  {
                                      /*
                                      policy.WithOrigins("http://localhost:3000",
                                                          "http://www.contoso.com");
                                      */
                                      policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                                  });
            });
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<MySQLConnectionService>();
            builder.Services.AddSingleton<CamStatusService>();
            builder.Services.AddScoped<PhotoUploadService>();
            builder.Services.AddTransient<ICamActionsService, CamActionsService>();
            builder.Services.AddTransient<ICameraRecordControl, CameraRecordControl>();

            //builder.Services.AddHostedService<MonitorNewRecordsBackgroundService>();
            builder.Services.AddHostedService<NewMonitoringRecords>();
            builder.Services.AddHostedService<PingBackgroundService>();
            //builder.Services.AddHostedService<RecordProcessingBackgroundService>();

            if (isService)
                builder.Services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddFilter("Microsoft", LogLevel.Warning);
                    builder.AddFilter("System", LogLevel.Error);
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            app.AddHandlerException(logger);
            app.UseCors(MyAllowSpecificOrigins);

            //app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseStaticFiles();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var camStatus = scope.ServiceProvider.GetRequiredService<CamStatusService>();
            };

            app.Run();
        }
    }
}