using System.Configuration;
using System.Net;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Indexes;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Serilog;
using Serilog.Events;

namespace DigitalLabels.Import.Config
{
    public static class DocumentStoreFactory
    {
        public static IDocumentStore Create()
        {
            using (Log.Logger.BeginTimedOperation("Create new instance of DocumentStore", "DocumentStoreFactory.Create", LogEventLevel.Debug))
            {
                // Connect to raven db instance
                var store = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["DatabaseUrl"],
                    DefaultDatabase = ConfigurationManager.AppSettings["DatabaseName"],
                    Credentials = new NetworkCredential(ConfigurationManager.AppSettings["DatabaseUserName"],
                        ConfigurationManager.AppSettings["DatabasePassword"],
                        ConfigurationManager.AppSettings["DatabaseDomain"])
                }.Initialize();

                // Ensure DB exists
                store.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(ConfigurationManager.AppSettings["DatabaseName"]);

                // Create core indexes and store facets
                IndexCreation.CreateIndexes(typeof(ManyNationsLabel_All).Assembly, store);

                // Ensure we have a application document
                using (var session = store.OpenSession())
                {
                    var application = session.Load<Application>(Constants.ApplicationId);

                    if (application == null)
                    {
                        Log.Logger.Information("Creating new application document");
                        session.Store(new Application());
                    }

                    session.SaveChanges();
                }

                return store;
            }
        }
    }
}
