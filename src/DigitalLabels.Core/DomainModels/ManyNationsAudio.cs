using System;
using System.Runtime.Serialization;

namespace DigitalLabels.Core.DomainModels
{
    public class ManyNationsAudio : IMedia
    {
        public long Irn { get; set; }

        public string Creator { get; set; }

        public string Description { get; set; }

        public string Source { get; set; }

        public string CopyrightHolder { get; set; }
        
        public string Url { get; set; }

        public string Order { get; set; }

        public DateTime DateModified { get; set; }
    }
}