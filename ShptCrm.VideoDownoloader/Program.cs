using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ShptCrm.VideoDownoloader;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mysqlx.Crud;

bool isService = !(Debugger.IsAttached || args.Contains("--console"));


var builder = new HostBuilder().ConfigureHostConfiguration(conf =>
{
    if(isService)
        conf.SetBasePath("C:\\GrabVideoService").AddJsonFile("appsettings.json").Build();
    else
        conf.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
}).ConfigureLogging(conf =>
{
    conf.Services.AddLogging(conf=>conf.AddConsole()).BuildServiceProvider();
}).ConfigureServices((constnet, services) =>
{
    services.AddHttpClient();
    services.AddHostedService<VideoGrabberHostedService>();

});

if(!isService)
    await builder.RunConsoleAsync();

if (isService && OperatingSystem.IsWindows())
{
    builder.UseWindowsService();
    await builder.Build().RunAsync();
}

if (isService && OperatingSystem.IsWindows())
    builder.UseSystemd();


