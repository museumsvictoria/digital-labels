using System.Linq;
using DigitalLabels.Core.DomainModels;
using Raven.Client.Indexes;

namespace DigitalLabels.Core.Indexes
{
    public class YulendjLabel_All : AbstractIndexCreationTask<YulendjLabel>
    {
        public YulendjLabel_All()
        {
            Map = labels => from label in labels
                            select new { };
        }
    }
}