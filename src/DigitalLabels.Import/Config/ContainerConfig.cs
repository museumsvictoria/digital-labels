using DigitalLabels.Import.Infrastructure;
using SimpleInjector;

namespace DigitalLabels.Import.Config
{
    public static class ContainerConfig
    {
        public static Container Initialize()
        {
            var container = new Container();

            // Register raven
            container.RegisterSingleton(DocumentStoreFactory.Create);

            var currentAssembly = typeof(Program).Assembly;

            container.RegisterCollection<ITask>(new [] { currentAssembly });
            container.Register(typeof(IImportFactory<>), new[] { currentAssembly });

            // Verify registrations
            container.Verify();

            return container;
        }
    }
}
