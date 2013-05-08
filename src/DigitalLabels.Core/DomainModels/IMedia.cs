using System;

namespace DigitalLabels.Core.DomainModels
{
    public interface IMedia
    {
        long Irn { get; }

        DateTime DateModified { get; set; }
    }
}