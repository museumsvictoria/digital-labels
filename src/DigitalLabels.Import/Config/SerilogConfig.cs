using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace DigitalLabels.Import.Config
{
    public static class SerilogConfig
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .WriteTo.RollingFile($"{AppDomain.CurrentDomain.BaseDirectory}\\logs\\{{Date}}.txt")
                .CreateLogger();
        }
    }
}
