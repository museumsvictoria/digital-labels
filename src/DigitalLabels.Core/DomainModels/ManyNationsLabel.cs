using System;
using System.Collections.Generic;

namespace DigitalLabels.Core.DomainModels
{
    public class ManyNationsLabel : DomainModel
    {
        public long Irn { get; set; }
        
        public string RegistrationNumber { get; set; }
        
        public string CommonName { get; set; }

        public string LanguageName { get; set; }

        public string DateMade { get; set; }

        public string PlaceMade { get; set; }
        
        public string Maker { get; set; }

        public string LanguageGroup  { get; set; }

        public string Region { get; set; }

        public string Materials { get; set; }

        public string Story { get; set; }

        public string StoryAuthor { get; set; }

        public string Segment { get; set; }

        public string Case { get; set; }

        public ICollection<ManyNationsImage> Images { get; set; }

        public ManyNationsAudio Audio { get; set; }

        public ManyNationsVideo Video { get; set; }

        public ManyNationsMap Map { get; set; }

        public DateTime DateModified { get; set; }
    }
}