using System.Collections.Generic;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Import.Infrastructure;
using IMu;
using Serilog;

namespace DigitalLabels.Import.Factories
{
    public abstract class ImportFactory<T> : IImportFactory<T>
    {
        public IList<T> Fetch()
        {
            // Cache Irns first
            var irns = this.FetchIrns();

            // Fetch data
            var records = new List<T>();
            var offset = 0;

            Log.Logger.Information("Fetching data {Name} {moduleName} irns", this.GetType().Name, this.ModuleName);

            while (true)
            {
                if (Program.ImportCanceled)
                    return records;

                using (var imuSession = ImuSessionProvider.CreateInstance(this.ModuleName))
                {
                    var cachedIrnsBatch = irns
                        .Skip(offset)
                        .Take(Constants.DataBatchSize)
                        .ToList();

                    if (cachedIrnsBatch.Count == 0)
                        break;

                    imuSession.FindKeys(cachedIrnsBatch);

                    var results = imuSession.Fetch("start", 0, -1, this.Columns);

                    Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);

                    records.AddRange(results.Rows.Select(this.Make));

                    offset += results.Count;

                    Log.Logger.Information("{Name} {moduleName} import progress... {Offset}/{TotalResults}", this.GetType().Name, this.ModuleName, offset, irns.Count);
                }
            }

            return records;
        }

        private IList<long> FetchIrns()
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            Log.Logger.Information("Caching {Name} {moduleName} irns", this.GetType().Name, this.ModuleName);

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(this.ModuleName))
            {               
                var hits = imuSession.FindTerms(this.Terms);

                Log.Logger.Information("Found {Hits} {moduleName} records where {@Terms}", hits, this.ModuleName, Terms.List);

                while (true)
                {
                    if (Program.ImportCanceled)
                        return cachedIrns;

                    var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, new[] { "irn" });

                    if (results.Count == 0)
                        break;

                    var irns = results.Rows.Select(x => x.GetLong("irn")).ToList();

                    cachedIrns.AddRange(irns);

                    offset += results.Count;

                    Log.Logger.Information("{Name} {moduleName} cache progress... {Offset}/{TotalResults}", this.GetType().Name, this.ModuleName, offset, hits);
                }
            }

            return cachedIrns;
        }

        public abstract string ModuleName { get; }
        public abstract string[] Columns { get; }
        public abstract Terms Terms { get; }
        public abstract T Make(Map map);
    }
}
