using System.Linq;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using Raven.Abstractions.Data;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Tasks
{
    public class GenerationsImportTask : ITask
    {
        private readonly IImportFactory<GenerationsLabel> generationsLabelImportFactory;
        private readonly IImportFactory<GenerationsQuote> generationsQuoteImportFactory;
        private readonly IDocumentStore store;

        public GenerationsImportTask(
            IDocumentStore store,
            IImportFactory<GenerationsLabel> generationsLabelImportFactory,
            IImportFactory<GenerationsQuote> generationsQuoteImportFactory)
        {
            this.store = store;
            this.generationsLabelImportFactory = generationsLabelImportFactory;
            this.generationsQuoteImportFactory = generationsQuoteImportFactory;
        }

        public void Execute()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Execute"))
            {
                // Get primary quote and create label
                var generationsLabels = generationsLabelImportFactory.Fetch();

                if (Program.ImportCanceled)
                    return;

                // Get supporting quotes
                var generationsQuotes = generationsQuoteImportFactory.Fetch();

                if (Program.ImportCanceled)
                    return;

                // Arrange supporting quotes into respective label
                foreach (var generationsLabel in generationsLabels)
                {
                    generationsLabel.SupportingQuotes = generationsQuotes.Where(x => x.PrimaryImageNarrativeIrn == generationsLabel.PrimaryQuote.NarrativeIrn).ToList();
                }

                using (var bulkInsert = store.BulkInsert(options: new BulkInsertOptions { OverwriteExisting = true }))
                {
                    foreach (var generationsLabel in generationsLabels)
                    {
                        bulkInsert.Store(generationsLabel);
                    }
                }
            }
        }
    }
}
