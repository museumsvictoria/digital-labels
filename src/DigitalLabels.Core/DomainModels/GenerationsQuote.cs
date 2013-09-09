using System;
using System.Runtime.Serialization;

namespace DigitalLabels.Core.DomainModels
{
    public class GenerationsQuote
    {
        public long Irn { get; set; }

        [IgnoreDataMember]
        public long NarrativeIrn { get; set; }

        [IgnoreDataMember]
        public long PrimaryImageNarrativeIrn { get; set; }

        public string RegistrationNumber { get; set; }

        public string HeaderText { get; set; }

        public string Caption { get; set; }

        public string PeopleDepicted { get; set; }

        public string LanguageGroup { get; set; }

        public string Place { get; set; }

        public string Date { get; set; }

        public string Photographer { get; set; }

        public string Quote { get; set; }

        public string QuoteAuthor { get; set; }

        public string QuoteSource { get; set; }

        public string QuoteName { get; set; }

        public GenerationsImage Image { get; set; }

        public DateTime DateModified { get; set; }        
    }
}