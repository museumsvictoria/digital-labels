using System;

namespace DigitalLabels.Core.DomainModels
{
    public class StandingStrongThumbnail
    {
        public long Irn { get; set; }

        public string Url { get; set; }

        public StandingStrongThumbnailType Type { get; set; }

        public DateTime DateModified { get; set; }
    }
}