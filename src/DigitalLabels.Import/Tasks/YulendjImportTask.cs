using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using Raven.Abstractions.Data;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Tasks
{
    public class YulendjImportTask : ITask
    {
        private readonly IImportFactory<YulendjLabel> yulendjLabelImportFactory;
        private readonly IDocumentStore store;

        public YulendjImportTask(
            IDocumentStore store,
            IImportFactory<YulendjLabel> yulendjLabelImportFactory)
        {
            this.store = store;
            this.yulendjLabelImportFactory = yulendjLabelImportFactory;
        }

        public void Execute()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Execute"))
            {
                using (var bulkInsert = store.BulkInsert(options: new BulkInsertOptions { OverwriteExisting = true }))
                {
                    foreach (var yulendjLabel in yulendjLabelImportFactory.Fetch())
                    {
                        bulkInsert.Store(yulendjLabel);
                    }
                }
            }
        }
    }
}
