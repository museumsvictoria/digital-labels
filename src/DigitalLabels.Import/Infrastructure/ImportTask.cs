using System;
using System.Collections.Generic;
using System.Linq;
using DigitalLabels.Core.Config;
using IMu;
using Serilog;

namespace DigitalLabels.Import.Infrastructure
{
    public abstract class ImportTask<T> : ITask
    {
        protected IEnumerable<long> CacheIrns(string moduleName, Terms searchTerms)
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
            {
                Log.Logger.Information("Caching {moduleName} irns", moduleName);

                var hits = imuSession.FindTerms(searchTerms);

                Log.Logger.Information("Found {Hits} {moduleName} records where {@Terms}", hits, moduleName, searchTerms.List);

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

                    Log.Logger.Information("{Name} {moduleName} cache progress... {Offset}/{TotalResults}", this.GetType().Name, moduleName, offset, hits);
                }
            }

            return cachedIrns;
        }

        protected IEnumerable<T> Fetch(IEnumerable<long> irns, string moduleName, string[] columns, Func<Map, T> makeRecordsFunc)
        {
            // Fetch data
            var records = new List<T>();
            var offset = 0;
            Log.Logger.Information("Fetching data");
            while (true)
            {
                if (Program.ImportCanceled)
                    return records;

                using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
                {
                    var cachedIrnsBatch = irns
                        .Skip(offset)
                        .Take(Constants.DataBatchSize)
                        .ToList();

                    if (cachedIrnsBatch.Count == 0)
                        break;

                    imuSession.FindKeys(cachedIrnsBatch);

                    var results = imuSession.Fetch("start", 0, -1, columns);

                    Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);

                    records.AddRange(results.Rows.Select(makeRecordsFunc));

                    offset += results.Count;

                    Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, irns.Count());
                }
            }

            return records;
        }

        public abstract void Execute();
    }
}
