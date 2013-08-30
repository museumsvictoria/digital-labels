using System;

namespace DigitalLabels.Core.DomainModels
{
    public class YulendjLabel : DomainModel
    {
        public long Irn { get; set; }

        public string Name { get; set; }

        public string Quote { get; set; }

        public string Biography { get; set; }

        public YulendjImage ProfileImage { get; set; }

        public YulendjImage TexturePanelImage { get; set; }

        public DateTime DateModified { get; set; }

        public string Order { get; set; }
    }
}