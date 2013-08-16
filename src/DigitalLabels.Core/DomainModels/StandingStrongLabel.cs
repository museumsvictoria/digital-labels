using System;

namespace DigitalLabels.Core.DomainModels
{
    public class StandingStrongLabel : DomainModel
    {
        public long Irn { get; set; }

        public string Story { get; set; }

        public string Quote { get; set; }

        public string QuoteSource { get; set; }        

        public StandingStrongImage Image { get; set; }

        public MediaAsset Thumbnail { get; set; }

        public DateTime DateModified { get; set; }

        public int Order { get; set; }
    }
}