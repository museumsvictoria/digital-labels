using System;

namespace DigitalLabels.Core.DomainModels
{
    public class ManyNationsImage : IMedia
    {
        public long Irn { get; set; }

        public string Creator { get; set; }

        public string Description { get; set; }

        public string Source { get; set; }

        public string CopyrightHolder { get; set; }

        public string MediumUrl { get; set; }

        public string LargeUrl { get; set; }

        public string Order { get; set; }

        public DateTime DateModified { get; set; }
    }
}