using System;
using System.Runtime.Serialization;

namespace DigitalLabels.Core.DomainModels
{
    public class ManyNationsMap : IMedia
    {
        public long Irn { get; set; }

        public string Url { get; set; }

        public DateTime DateModified { get; set; }
    }
}