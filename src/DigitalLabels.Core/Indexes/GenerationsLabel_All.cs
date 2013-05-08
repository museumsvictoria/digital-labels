using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigitalLabels.Core.DomainModels;
using Raven.Client.Indexes;

namespace DigitalLabels.Core.Indexes
{
    public class GenerationsLabel_All : AbstractIndexCreationTask<GenerationsLabel>
    {
        public GenerationsLabel_All()
        {
            Map = labels => from label in labels
                            select new { };
        }
    }
}