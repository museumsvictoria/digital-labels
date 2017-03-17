using System;
using System.Configuration;
using Serilog;

namespace DigitalLabels.Import.Config
{
    public static class SerilogConfig
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Environment", ConfigurationManager.AppSettings["SeqEnvironment"])
                .Enrich.WithProperty("Application", "Digital Labels Import")
                .WriteTo.Seq(ConfigurationManager.AppSettings["SeqUrl"])
                .WriteTo.ColoredConsole()
                .WriteTo.RollingFile($"{AppDomain.CurrentDomain.BaseDirectory}\\logs\\{{Date}}.txt")
                .CreateLogger();
        }
    }
}
