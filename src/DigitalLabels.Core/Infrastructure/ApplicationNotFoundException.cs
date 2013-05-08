using System;

namespace DigitalLabels.Core.Infrastructure
{
    public class ApplicationNotFoundException : Exception
    {
        public ApplicationNotFoundException()
            : base("Application not found")
        {
        }
    }
}