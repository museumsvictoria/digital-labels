using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using Raven.Client.Linq;

namespace DigitalLabels.Core.Extensions
{
    public static class QueryableExtensions
    {
        private const string ExceedsLimitMessage = @"This operation can't be completed because it would require exceeding the session request limit.
You should probably narrow the scope of the query, but you can also increase the request limit with session.Advanced.MaxNumberOfRequestsPerSession.
Alternatively, you can pass expandSessionRequestLimitAsNeededWhichMightBeDangerous = true and all results will be returned, but use this cautiously,
as it could needlessly consume a lot of resources for a very large result set.";

        /// <summary>
        /// Executes the query and iterates over every page, returning all results.
        /// </summary>
        public static IEnumerable<T> GetAllResultsWithPaging<T>(this IRavenQueryable<T> queryable, int pageSize = 128,
                                                                bool expandSessionRequestLimitAsNeededWhichMightBeDangerous = false)
        {
            var skipped = 0;
            var total = 0;
            var page = 0;

            var session = ((RavenQueryInspector<T>)queryable).Session;

            queryable = queryable.SyncCutoff();

            while (true)
            {
                RavenQueryStatistics stats;
                var results = queryable.Statistics(out stats)
                                       .Skip(page * pageSize + skipped)
                                       .Take(pageSize);

                foreach (var item in results)
                {
                    yield return item;
                    total++;
                }

                skipped += stats.SkippedResults;

                if (total + skipped >= stats.TotalResults)
                    break;

                if (expandSessionRequestLimitAsNeededWhichMightBeDangerous)
                    session.MaxNumberOfRequestsPerSession++;
                else if ((stats.TotalResults / pageSize) + session.NumberOfRequests > session.MaxNumberOfRequestsPerSession)
                    throw new InvalidOperationException(ExceedsLimitMessage);

                page++;
            }
        }

        public static IRavenQueryable<T> SyncCutoff<T>(this IRavenQueryable<T> queryable)
        {
            return queryable.Customize(x => x.TransformResults((query, results) =>
            {
                if (query.Cutoff.HasValue)
                    queryable.Customize(z => z.WaitForNonStaleResultsAsOf(query.Cutoff.Value));

                return results;
            }));
        }
    }

}
