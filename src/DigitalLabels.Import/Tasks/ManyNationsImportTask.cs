using System.Collections.Generic;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using IMu;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Tasks
{
    public class ManyNationsImportTask : ImportTask<ManyNationsLabel>
    {
        private readonly IImportFactory<ManyNationsLabel> manyNationsLabelImportFactory;
        private readonly IDocumentStore documentStore;

        ManyNationsImportTask(
            IDocumentStore documentStore,
            IImportFactory<ManyNationsLabel> manyNationsLabelImportFactory)
        {
            this.documentStore = documentStore;
            this.manyNationsLabelImportFactory = manyNationsLabelImportFactory;
        }

        public override void Execute()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Execute"))
            {
                var cachedIrns = this.CacheIrns(manyNationsLabelImportFactory.ModuleName, manyNationsLabelImportFactory.Terms);

                var records = this.Fetch(cachedIrns, manyNationsLabelImportFactory.ModuleName, manyNationsLabelImportFactory.Columns, manyNationsLabelImportFactory.Make);


            }
        }
    }
}
