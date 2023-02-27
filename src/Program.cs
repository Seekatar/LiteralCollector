
using LiteralCollector;
using static System.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seekatar.Tools;
using Serilog;
using Serilog.Configuration;
using System.Diagnostics;
using LiteralCollector.Database;

Environment.SetEnvironmentVariable("NETCORE_ENVIRONMENT", "Development");
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
var configuration = new ConfigurationBuilder()
    .AddSharedDevSettings()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection();

Log.Logger = new LoggerConfiguration()
    .ReadFrom
    .Configuration(configuration)
    .CreateLogger();

serviceCollection.AddLogging(configure => configure.AddSerilog());

serviceCollection.AddOptions<CollectorOptions>()
    .Bind(configuration.GetSection("CollectorOptions"));

serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddSingleton<IPersistence,Persistence>();
serviceCollection.AddSingleton<IPersistence,EfPersistence>();
serviceCollection.AddSingleton<Collector>();

var provider = serviceCollection.BuildServiceProvider();



var sw = new Stopwatch();
sw.Start();

var path = ".";
if (args.Length > 0)
{
    if (Directory.Exists(args[0]))
        path = args[0];
    else
        throw new ArgumentException("First parameter must be valid path");
}

int fileCount = 0;
using var lc = provider.GetRequiredService<Collector>();

lc.Process(path);

fileCount = lc.FileCount;


sw.Stop();
WriteLine($"Processed {fileCount} files in {sw.Elapsed.TotalMinutes:0.00} minutes");

if (Debugger.IsAttached)
{
    Write("Press enter to exit ");
    ReadLine();
}
