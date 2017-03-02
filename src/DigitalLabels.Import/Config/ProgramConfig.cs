using System;
using Serilog;

namespace DigitalLabels.Import.Config
{
    public static class ProgramConfig
    {
        public static void Initialize()
        {
            // Set up Ctrl+C handling 
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Logger.Warning("Canceling export");

                eventArgs.Cancel = true;
                Program.ImportCanceled = true;
            };

            // Log any exceptions that are not handled
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Unhandled Exception occured in export");
        }
    }
}
