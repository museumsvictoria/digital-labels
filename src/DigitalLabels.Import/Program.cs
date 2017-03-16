using DigitalLabels.Import.Config;
using DigitalLabels.Import.Infrastructure;

namespace DigitalLabels.Import
{
    static class Program
    {
        public static volatile bool ImportCanceled;

        static void Main(string[] args)
        {
            // Configure Program
            ProgramConfig.Initialize();

            // Configure Serilog
            SerilogConfig.Initialize();

            // Configure DI container
            var container = ContainerConfig.Initialize();

            // Run all tasks
            container.GetInstance<TaskRunner>().ExecuteAll();
        }
    }
}