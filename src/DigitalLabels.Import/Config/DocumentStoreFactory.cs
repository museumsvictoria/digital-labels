using System;
using System.Configuration;
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
                var documentStore = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["DatabaseUrl"],
                    DefaultDatabase = ConfigurationManager.AppSettings["DatabaseName"]
                }.Initialize();

                // Ensure DB exists
                documentStore.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(ConfigurationManager.AppSettings["DatabaseName"]);

                // Create core indexes and store facets
                IndexCreation.CreateIndexes(typeof(ManyNationsLabel_All).Assembly, documentStore);

                // Ensure we have a application document
                using (var documentSession = documentStore.OpenSession())
                {
                    var application = documentSession.Load<Application>(Constants.ApplicationId);

                    if (application == null)
                    {
                        Log.Logger.Information("Creating new application document");
                        documentSession.Store(new Application());
                    }

                    documentSession.SaveChanges();
                }

                return documentStore;
            }
        }
    }
}
