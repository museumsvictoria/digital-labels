using System;

namespace DigitalLabels.Core.DomainModels
{
    public class GenerationsImage
    {
        public long Irn { get; set; }

        public string Source { get; set; }

        public string Acknowledgements { get; set; }

        public string MediumUrl { get; set; }

        public string LargeUrl { get; set; }

        public string Order { get; set; }

        public DateTime DateModified { get; set; }
    }
}