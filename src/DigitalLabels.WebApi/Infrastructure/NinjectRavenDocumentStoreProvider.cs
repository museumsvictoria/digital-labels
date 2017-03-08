using System.Configuration;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Indexes;
using Ninject.Activation;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Indexes;

namespace DigitalLabels.WebApi.Infrastructure
{
    public class NinjectRavenDocumentStoreProvider : Provider<IDocumentStore>
    {
        protected override IDocumentStore CreateInstance(IContext ctx)
        {
            var documentStore = new DocumentStore
            {
                Url = ConfigurationManager.AppSettings["DatabaseUrl"]
            };

            var hasDefaultDatabase = !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["DatabaseName"]);

            if (hasDefaultDatabase)
            {
                documentStore.DefaultDatabase = ConfigurationManager.AppSettings["DatabaseName"];
            }

            documentStore.Initialize();

            if (hasDefaultDatabase)
            {
                documentStore.DatabaseCommands.EnsureDatabaseExists(ConfigurationManager.AppSettings["DatabaseName"]);
            }

            // Add our indexes
            IndexCreation.CreateIndexes(typeof(ManyNationsLabel_All).Assembly, documentStore);

            // Ensure we have an application document
            using (var documentSession = documentStore.OpenSession())
            {
                var application = documentSession.Load<Application>(Constants.ApplicationId);
                
                if (application == null)
                {
                    documentSession.Store(new Application());
                    documentSession.SaveChanges();
                }                
            }                       

            return documentStore;
        }
    }
}