using System.Collections.Generic;

namespace DigitalLabels.Core.DomainModels
{
    public class GenerationsLabel : DomainModel
    {
        public string Theme { get; set; }

        public GenerationsQuote PrimaryQuote { get; set; }

        public ICollection<GenerationsQuote> SupportingQuotes { get; set; }
    }
}