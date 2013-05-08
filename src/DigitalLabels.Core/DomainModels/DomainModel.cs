using System.Runtime.Serialization;

namespace DigitalLabels.Core.DomainModels
{
    public abstract class DomainModel
    {
        [IgnoreDataMember]
        public string Id { get; set; }
    }
}