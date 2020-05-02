using System;
using JoySoftware.HomeAssistant.NetDaemon.DaemonRunner.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace cs
{
    class Program
    {
        private static LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch();

        public static void Main(string[] args)
        {
            try
            {
                // Setup serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                    .CreateLogger();


                var envLogLevel = Environment.GetEnvironmentVariable("HASS_LOG_LEVEL");
                _levelSwitch.MinimumLevel = envLogLevel switch
                {
                    "info" => LogEventLevel.Information,
                    "debug" => LogEventLevel.Debug,
                    "error" => LogEventLevel.Error,
                    "warning" => LogEventLevel.Warning,
                    "trace" => LogEventLevel.Verbose,
                    _ => LogEventLevel.Information
                };

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Failed to start host...");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices(services => { services.AddHostedService<RunnerService>(); });

    }
}
