
using LiteralCollector;
using static System.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seekatar.Tools;
using Serilog;
using System.Diagnostics;

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

if (args.Length < 2 || !Directory.Exists(args[0]) || !Directory.Exists(args[1]))
{
    WriteLine("Supply Fully Qualified path to scan and its fully qualified base");
    return 9;
}

int fileCount = 0;
using var lc = provider.GetRequiredService<Collector>();

lc.Process(args[0], args[1]);

fileCount = lc.FileCount;


sw.Stop();
WriteLine($"Processed {fileCount} files in {sw.Elapsed.TotalMinutes:0.00} minutes");

if (Debugger.IsAttached)
{
    Write("Press enter to exit ");
    ReadLine();
}

return 0;