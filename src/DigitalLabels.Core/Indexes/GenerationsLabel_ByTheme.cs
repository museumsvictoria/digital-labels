using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigitalLabels.Core.DomainModels;
using Raven.Client.Indexes;

namespace DigitalLabels.Core.Indexes
{
    public class GenerationsLabel_ByTheme : AbstractIndexCreationTask<GenerationsLabel>
    {
        public GenerationsLabel_ByTheme()
        {
            Map = labels => from label in labels
                            select new { label.Theme };
        }
    }
}