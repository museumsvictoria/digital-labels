using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using SimpleInjector;

namespace DigitalLabels.Import.Config
{
    public static class ContainerConfig
    {
        public static Container Initialize()
        {
            var container = new Container();

            // Register raven
            container.RegisterSingleton(RavenDocumentStoreFactory.Create);

            // Verify registrations
            container.Verify();

            return container;
        }
    }
}
