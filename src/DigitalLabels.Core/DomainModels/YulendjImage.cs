using System;

namespace DigitalLabels.Core.DomainModels
{
    public class YulendjImage
    {
        public long Irn { get; set; }

        public string Description { get; set; }

        public string Photographer { get; set; }

        public string Source { get; set; }

        public string Url { get; set; }

        public DateTime DateModified { get; set; }
    }
}