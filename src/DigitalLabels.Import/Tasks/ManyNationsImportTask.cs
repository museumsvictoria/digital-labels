using System.Collections.Generic;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using IMu;
using Raven.Abstractions.Data;
using Raven.Abstractions.Extensions;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Tasks
{
    public class ManyNationsImportTask : ImportTask<ManyNationsLabel>
    {
        private readonly IImportFactory<ManyNationsLabel> manyNationsLabelImportFactory;
        private readonly IDocumentStore store;

        public ManyNationsImportTask(
            IDocumentStore store,
            IImportFactory<ManyNationsLabel> manyNationsLabelImportFactory)
        {
            this.store = store;
            this.manyNationsLabelImportFactory = manyNationsLabelImportFactory;
        }

        public override void Execute()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Execute"))
            {
                var cachedIrns = this.CacheIrns(manyNationsLabelImportFactory.ModuleName, manyNationsLabelImportFactory.Terms);

                var labels = this.Fetch(cachedIrns, manyNationsLabelImportFactory.ModuleName, manyNationsLabelImportFactory.Columns, manyNationsLabelImportFactory.Make);

                using (var bulkInsert = store.BulkInsert(options: new BulkInsertOptions { OverwriteExisting = true }))
                {
                    foreach (var label in labels)
                    {
                        bulkInsert.Store(label);
                    }
                }
            }
        }
    }
}
