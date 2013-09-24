using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Indexes;
using DigitalLabels.Core.Infrastructure;
using DigitalLabels.Import.Factories;
using IMu;
using NLog;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Constants = DigitalLabels.Core.Config.Constants;
using Raven.Client.Extensions;

namespace DigitalLabels.Import
{
    static class Program
    {
        private static IDocumentStore _documentStore;
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static Session _session;

        static void Main(string[] args)
        {
            Initialize();

            Import();

            Dispose();
        }

        private static void Import()
        {
            var dateRun = DateTime.Now;
            bool hasFailed = false;

            try
            {
                _log.Debug("Data Import begining");

                var documentSession = _documentStore.OpenSession();
                var application = documentSession.Load<Application>(Constants.ApplicationId);

                if (application == null)
                    throw new ApplicationNotFoundException();

                if (!application.DataImportRunning)
                {
                    application.RunDataImport();
                    documentSession.SaveChanges();
                    documentSession.Dispose();
                                        
                    RunManyNationsImport();                    
                    RunGenerationsImport();
                    RunYulendjImport();
                    RunStandingStrongImport();
                }
            }
            catch (Exception exception)
            {
                hasFailed = true;
                _log.Debug(exception.ToString);
            }

            using (var documentSession = _documentStore.OpenSession())
            {
                var application = documentSession.Load<Application>(Constants.ApplicationId);

                if (application.DataImportCancelled || hasFailed)
                {
                    _log.Debug("Data import finished (cancelled or failed)");
                    application.DataImportFinished();
                }
                else
                {
                    _log.Debug("Data import finished succesfully");
                    application.DataImportSuccess(dateRun);
                }

                documentSession.SaveChanges();
            }
        }

        private static void RunManyNationsImport()
        {
            _log.Debug("Begining Many Nations Label import");

            var labels = new List<ManyNationsLabel>();

            #region data

            // Get search ready
            var catalogue = new Module("ecatalogue", _session);

            // Perform search
            var stopwatch = Stopwatch.StartNew();
            var hits = catalogue.FindTerms(ManyNationsImuFactory.GetImportTerms());
            stopwatch.Stop();
            _log.Debug("Finished Catalogue Emu Search in {0:#,#} ms. {1} Hits", stopwatch.ElapsedMilliseconds, hits);

            var count = 0;
            stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (documentSession.Load<Application>(Constants.ApplicationId).DataImportCancelled)
                    {
                        _log.Debug("Cancel command recieved stopping Data import");
                        return;
                    }

                    var results = catalogue.Fetch("start", count, Constants.DataBatchSize, ManyNationsImuFactory.GetImportColumns());

                    if (results.Count == 0)
                        break;

                    // Create labels
                    var newLabels = results.Rows.Select(ManyNationsLabelFactory.MakeLabel).ToList();
                    
                    // Keep labels for file import
                    labels.AddRange(newLabels);

                    count += results.Count;
                    _log.Debug("Many Nations Label import progress... {0}/{1}", count, hits);
                }
            }

            #endregion

            #region persist

            _log.Debug("Deleting existing Many Nations labels");
            
            // Delete all existing data
            using (var documentSession = _documentStore.OpenSession())
            {
                _documentStore.DatabaseCommands.DeleteByIndex("ManyNationsLabel/All", new IndexQuery(), false);
                documentSession.SaveChanges();
            }

            // Ensure we delete all labels
            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    RavenQueryStatistics statistics;
                    documentSession.Query<ManyNationsLabel>()
                                   .Statistics(out statistics)
                                   .ToArray();

                    if (statistics.TotalResults == 0)
                    {
                        break;
                    }

                    Thread.Sleep(200);
                }
            }

            _log.Debug("Saving Many Nations labels");

            // Insert new labels
            using (var bulkInsert = _documentStore.BulkInsert())
            {
                foreach (var label in labels)
                {
                    bulkInsert.Store(label);
                }
            }

            #endregion

            stopwatch.Stop();
            _log.Debug("Many Nations import finished, total Time: {0:0.00} Mins", stopwatch.Elapsed.TotalMinutes);
        }

        private static void RunGenerationsImport()
        {
            var primaryLabels = new List<GenerationsLabel>();
            var supportingImages = new List<GenerationsQuote>();

            #region data

            _log.Debug("Begining Generations (primary) import");

            // Get search ready
            var catalogue = new Module("ecatalogue", _session);

            // Perform search
            var stopwatch = Stopwatch.StartNew();
            var hits = catalogue.FindTerms(GenerationsImuFactory.GetPrimaryImportTerms());
            stopwatch.Stop();
            _log.Debug("Finished Catalogue Emu Search in {0:#,#} ms. {1} Hits", stopwatch.ElapsedMilliseconds, hits);

            var count = 0;
            stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (documentSession.Load<Application>(Constants.ApplicationId).DataImportCancelled)
                    {
                        _log.Debug("Cancel command recieved stopping Data import");
                        return;
                    }

                    var results = catalogue.Fetch("start", count, Constants.DataBatchSize, GenerationsImuFactory.GetPrimaryImportColumns());

                    //Create Images/Labels
                    primaryLabels.AddRange(results.Rows.Select(GenerationsLabelFactory.MakeLabel));

                    if (results.Count == 0)
                        break;

                    count += results.Count;
                    _log.Debug("Generations (primary) Label import progress... {0}/{1}", count, hits);
                }
            }

            _log.Debug("Begining Generations (supporting) import");

            // Get search ready
            var narrative = new Module("enarratives", _session);

            // Perform search
            stopwatch = Stopwatch.StartNew();
            hits = narrative.FindTerms(GenerationsImuFactory.GetSupportingImportTerms());
            stopwatch.Stop();
            _log.Debug("Finished Narrative Emu Search in {0:#,#} ms. {1} Hits", stopwatch.ElapsedMilliseconds, hits);

            count = 0;
            stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (documentSession.Load<Application>(Constants.ApplicationId).DataImportCancelled)
                    {
                        _log.Debug("Cancel command recieved stopping Data import");
                        return;
                    }

                    var results = narrative.Fetch("start", count, Constants.DataBatchSize, GenerationsImuFactory.GetSupportingImportColumns());

                    //Create Images/Labels
                    supportingImages.AddRange(results.Rows.Select(GenerationsLabelFactory.MakeSupportingQuote));

                    if (results.Count == 0)
                        break;

                    count += results.Count;
                    _log.Debug("Generations (supporting) Label import progress... {0}/{1}", count, hits);
                }
            }

            // Arrange images into labels.
            foreach (var primaryLabel in primaryLabels)
            {
                primaryLabel.SupportingQuotes = supportingImages.Where(x => x.PrimaryImageNarrativeIrn == primaryLabel.PrimaryQuote.NarrativeIrn).ToList();
            }

            #endregion

            #region persist

            _log.Debug("Deleting existing Generations labels");

            // Delete all existing data
            using (var documentSession = _documentStore.OpenSession())
            {
                _documentStore.DatabaseCommands.DeleteByIndex("GenerationsLabel/All", new IndexQuery(), false);
                documentSession.SaveChanges();
            }

            // Ensure we delete all labels
            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    RavenQueryStatistics statistics;
                    documentSession.Query<GenerationsLabel>()
                                   .Statistics(out statistics)
                                   .ToArray();

                    if (statistics.TotalResults == 0)
                    {
                        break;
                    }

                    Thread.Sleep(200);
                }
            }

            _log.Debug("Saving Generations labels");

            // Insert new labels
            using (var bulkInsert = _documentStore.BulkInsert())
            {
                foreach (var primaryLabel in primaryLabels)
                {
                    bulkInsert.Store(primaryLabel);
                }
            }

            #endregion

            stopwatch.Stop();
            _log.Debug("Generations import finished, total Time: {0:0.00} Mins", stopwatch.Elapsed.TotalMinutes);
        }

        private static void RunYulendjImport()
        {
            _log.Debug("Begining Yulendj Label import");

            var labels = new List<YulendjLabel>();

            #region data

            // Get search ready
            var catalogue = new Module("enarratives", _session);

            // Perform search
            var stopwatch = Stopwatch.StartNew();
            var hits = catalogue.FindTerms(YulendjImuFactory.GetImportTerms());
            stopwatch.Stop();
            _log.Debug("Finished Narrative Emu Search in {0:#,#} ms. {1} Hits", stopwatch.ElapsedMilliseconds, hits);

            var count = 0;
            stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (documentSession.Load<Application>(Constants.ApplicationId).DataImportCancelled)
                    {
                        _log.Debug("Cancel command recieved stopping Data import");
                        return;
                    }

                    var results = catalogue.Fetch("start", count, Constants.DataBatchSize, YulendjImuFactory.GetImportColumns());

                    if (results.Count == 0)
                        break;

                    // Create labels
                    var newLabels = results.Rows.Select(YulendjLabelFactory.MakeLabel).ToList();

                    // Keep labels for file import
                    labels.AddRange(newLabels);

                    count += results.Count;
                    _log.Debug("Yulendj import progress... {0}/{1}", count, hits);
                }
            }

            #endregion

            #region persist

            _log.Debug("Deleting existing Yulendj labels");

            // Delete all existing data
            using (var documentSession = _documentStore.OpenSession())
            {
                _documentStore.DatabaseCommands.DeleteByIndex("YulendjLabel/All", new IndexQuery(), false);
                documentSession.SaveChanges();
            }

            // Ensure we delete all labels
            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    RavenQueryStatistics statistics;
                    documentSession.Query<YulendjLabel>()
                                   .Statistics(out statistics)
                                   .ToArray();

                    if (statistics.TotalResults == 0)
                    {
                        break;
                    }

                    Thread.Sleep(200);
                }
            }

            _log.Debug("Saving Yulendj labels");

            // Insert new labels
            using (var bulkInsert = _documentStore.BulkInsert())
            {
                foreach (var label in labels)
                {
                    bulkInsert.Store(label);
                }
            }

            #endregion

            stopwatch.Stop();
            _log.Debug("Yulendj import finished, total Time: {0:0.00} Mins", stopwatch.Elapsed.TotalMinutes);
        }

        private static void RunStandingStrongImport()
        {
            _log.Debug("Begining Standing Strong Label import");

            var labels = new List<StandingStrongLabel>();

            #region data

            // Get search ready
            var catalogue = new Module("enarratives", _session);

            // Perform search
            var stopwatch = Stopwatch.StartNew();
            var hits = catalogue.FindTerms(StandingStrongImuFactory.GetImportTerms());
            stopwatch.Stop();
            _log.Debug("Finished Narrative Emu Search in {0:#,#} ms. {1} Hits", stopwatch.ElapsedMilliseconds, hits);

            var count = 0;
            stopwatch = Stopwatch.StartNew();

            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (documentSession.Load<Application>(Constants.ApplicationId).DataImportCancelled)
                    {
                        _log.Debug("Cancel command recieved stopping Data import");
                        return;
                    }

                    var results = catalogue.Fetch("start", count, Constants.DataBatchSize, StandingStrongImuFactory.GetImportColumns());

                    if (results.Count == 0)
                        break;

                    // Create labels
                    var newLabels = results.Rows.Select(StandingStrongLabelFactory.MakeLabel).ToList();

                    // Keep labels for file import
                    labels.AddRange(newLabels);

                    count += results.Count;
                    _log.Debug("Standing Strong import progress... {0}/{1}", count, hits);
                }
            }

            #endregion

            #region persist

            _log.Debug("Deleting existing Standing Strong labels");

            // Delete all existing data
            using (var documentSession = _documentStore.OpenSession())
            {
                _documentStore.DatabaseCommands.DeleteByIndex("StandingStrongLabel/All", new IndexQuery(), false);
                documentSession.SaveChanges();
            }

            // Ensure we delete all labels
            while (true)
            {
                using (var documentSession = _documentStore.OpenSession())
                {
                    RavenQueryStatistics statistics;
                    documentSession.Query<StandingStrongLabel>()
                                   .Statistics(out statistics)
                                   .ToArray();

                    if (statistics.TotalResults == 0)
                    {
                        break;
                    }

                    Thread.Sleep(200);
                }
            }


            _log.Debug("Saving Standing Strong labels");

            using (var bulkInsert = _documentStore.BulkInsert())
            {
                foreach (var label in labels)
                {
                    bulkInsert.Store(label);
                }
            }

            #endregion

            stopwatch.Stop();
            _log.Debug("Standing Strong import finished, total Time: {0:0.00} Mins", stopwatch.Elapsed.TotalMinutes);
        }

        private static void Initialize()
        {
            // Connect to raven db instance
            _log.Debug("Initializing document store");
            _documentStore = new DocumentStore
            {
                Url = ConfigurationManager.AppSettings["DatabaseUrl"],
                DefaultDatabase = ConfigurationManager.AppSettings["DatabaseName"]
            }.Initialize();

            // Ensure DB exists
            _documentStore.DatabaseCommands.EnsureDatabaseExists(ConfigurationManager.AppSettings["DatabaseName"]);

            // Add indexes
            IndexCreation.CreateIndexes(typeof(ManyNationsLabel_All).Assembly, _documentStore);

            // Connect to Imu
            _log.Debug("Connecting to Imu server: {0}:{1}", ConfigurationManager.AppSettings["EmuServerHost"], ConfigurationManager.AppSettings["EmuServerPort"]);
            _session = new Session(ConfigurationManager.AppSettings["EmuServerHost"], int.Parse(ConfigurationManager.AppSettings["EmuServerPort"]));
            _session.Connect();

            // Ensure we have a application document
            using (var documentSession = _documentStore.OpenSession())
            {
                var application = documentSession.Load<Application>(Constants.ApplicationId);

                if (application == null)
                {
                    application = new Application();
                    documentSession.Store(application);
                }

                documentSession.SaveChanges();
            }
        }

        private static void Dispose()
        {
            _documentStore.Dispose();
        }
    }
}