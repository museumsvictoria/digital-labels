using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using DigitalLabels.Import.Utilities;
using ImageMagick;
using IMu;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Factories
{
    public class GenerationsLabelImportFactory : ImportFactory<GenerationsLabel>
    {
        private readonly IDocumentStore store;
        private Terms terms;

        public GenerationsLabelImportFactory(IDocumentStore store)
        {
            this.store = store;
        }

        public override string ModuleName => "ecatalogue";

        public override string[] Columns => new[]
        {
            "irn",
            "ColRegPrefix",
            "ColRegNumber",
            "ColRegPart",
            "ClaTertiaryClassification",
            "AdmDateModified",
            "AdmTimeModified",
            "captions=[DesCaption_tab,DesPurpose_tab]",
            "associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab]",
            "media=MulMultiMediaRef_tab.(irn,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
            "narrative=<enarratives:ObjObjectsRef_tab>.(irn,NarTitle,DetPurpose_tab,DepPeople0,NarNarrative,interviews=[name=IntIntervieweeRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),IntInterviewLocation_tab],AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
        };

        public override Terms Terms
        {
            get
            {
                if (terms != null)
                    return terms;

                terms = new Terms();

                terms.Add("MdaDataSets_tab", "Bunjilaka Digital Label");
                terms.Add("ClaSecondaryClassification", "Generations");
                terms.Add("ClaObjectName", "primary");
                terms.Add("AdmPublishWebNoPassword", "Yes");

                using (var session = store.OpenSession())
                {
                    var application = session.Load<Application>(Constants.ApplicationId);
                    if (application.LastCompleted.HasValue)
                        terms.Add("AdmDateModified", application.LastCompleted.Value.ToString("MMM dd yyyy"), ">=");
                }

                return terms;
            }
        }    

        public override GenerationsLabel Make(Map map)
        {
            var label = new GenerationsLabel
            {
                Id = "generationslabels/" + map.GetString("irn"),
                Theme = map.GetString("ClaTertiaryClassification"),
                PrimaryQuote = new GenerationsQuote
                {
                    Irn = long.Parse(map.GetString("irn")),
                    DateModified = DateTime.ParseExact($"{map.GetString("AdmDateModified")} {map.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")),
                    RegistrationNumber = map.GetString("ColRegPart") != null ? $"{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}.{map.GetString("ColRegPart")}" : $"{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}"
                }
            };

            // Caption/HeaderText
            foreach (var caption in map.GetMaps("captions"))
            {
                switch (caption.GetString("DesPurpose_tab"))
                {
                    case "Bunjilaka Header Text 2012":
                        label.PrimaryQuote.HeaderText = caption.GetString("DesCaption_tab");
                        break;
                    case "Bunjilaka Caption 2012":
                        label.PrimaryQuote.Caption = caption.GetString("DesCaption_tab");
                        break;
                }
            }

            // Associations
            foreach (var association in map.GetMaps("associations"))
            {
                switch (association.GetString("AssAssociationType_tab"))
                {
                    case "Clan/Language Group":
                        var languageGroup = association.GetMap("name");
                        if (languageGroup != null)
                        {
                            label.PrimaryQuote.LanguageGroup = languageGroup.GetString("NamPartyType") == "Person" ? languageGroup.GetStrings("NamOtherNames_tab").FirstOrDefault() : languageGroup.GetString("NamOrganisation");
                        }
                        break;
                    case "Place Depicted":
                        var locality = association.GetString("AssAssociationLocality_tab");
                        var state = association.GetString("AssAssociationState_tab");
                        label.PrimaryQuote.Place = $"{locality} {state}";
                        break;
                    case "Date Depicted":
                        label.PrimaryQuote.Date = association.GetString("AssAssociationDate_tab");
                        break;
                    case "Photographer":
                        var photographer = association.GetMap("name");
                        if (photographer != null)
                        {
                            label.PrimaryQuote.Photographer = photographer.GetString("NamPartyType") == "Person" ? photographer.GetStrings("NamOtherNames_tab").FirstOrDefault() : photographer.GetString("NamOrganisation");
                        }
                        break;
                }
            }

            // Media
            var medias = map.GetMaps("media");
            foreach (var media in medias)
            {
                if (media != null &&
                    string.Equals(media.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                    media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                {
                    var irn = long.Parse(media.GetString("irn"));
                    var type = media.GetString("MulMimeType");
                    var elements = media.GetStrings("MdaElement_tab");
                    var freeTexts = media.GetStrings("MdaFreeText_tab");
                    var dateModified = DateTime.ParseExact($"{media.GetString("AdmDateModified")} {media.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                    var length = Arrays.FindLongestLength(elements, freeTexts);

                    string source = string.Empty, acknowledgements = string.Empty, order = string.Empty;

                    for (var i = 0; i < length; i++)
                    {
                        var element = i < elements.Length ? elements[i] : null;
                        var freeText = i < freeTexts.Length ? freeTexts[i] : null;

                        switch (element)
                        {
                            case "dcSource":
                                source = freeText;
                                break;
                            case "Acknowledgement":
                                acknowledgements = freeText;
                                break;
                            case "Image Order":
                                order = freeText;
                                break;
                        }
                    }

                    if (type == "image" &&
                        MediaHelper.TrySaveMedia(irn, imageMediaJobs))
                    {
                        label.PrimaryQuote.Image = new GenerationsImage
                        {
                            Acknowledgements = acknowledgements,
                            DateModified = dateModified,
                            Irn = irn,
                            LargeUrl = PathHelper.GetUrlPath(irn, FileFormatType.Jpg, "large"),
                            MediumUrl = PathHelper.GetUrlPath(irn, FileFormatType.Jpg, "medium"),
                            Order = order,
                            Source = source
                        };
                    }
                }
            }

            // Narrative
            var narrative = map
                .GetMaps("narrative")
                .FirstOrDefault(x => x.GetStrings("DetPurpose_tab").Any(y => y.Contains("Exhibition - Bunjilaka Generations Digital Label")) && string.Equals(x.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) && x.GetString("NarTitle").ToLower().Contains("primary"));

            if (narrative != null)
            {
                var dateTime = DateTime.ParseExact($"{narrative.GetString("AdmDateModified")} {narrative.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                if (dateTime > label.PrimaryQuote.DateModified)
                    label.PrimaryQuote.DateModified = dateTime;

                label.PrimaryQuote.NarrativeIrn = long.Parse(narrative.GetString("irn"));
                label.PrimaryQuote.PeopleDepicted = narrative.GetStrings("DepPeople0").FirstOrDefault();
                label.PrimaryQuote.Quote = HtmlConverter.HtmlToText(narrative.GetString("NarNarrative"));

                var interviews = narrative.GetMaps("interviews");

                foreach (var interview in interviews)
                {
                    label.PrimaryQuote.QuoteSource = interview.GetString("IntInterviewLocation_tab");

                    var interviewer = interview.GetMap("name");
                    if (interviewer != null)
                    {
                        label.PrimaryQuote.QuoteAuthor = interviewer.GetString("NamPartyType") == "Person" ? interviewer.GetStrings("NamOtherNames_tab").FirstOrDefault() : interviewer.GetString("NamOrganisation");
                    }
                }
            }

            Log.Logger.Debug("Completed {id} creation", label.Id);

            return label;
        }

        private readonly IEnumerable<MediaJob> imageMediaJobs = new[]
{
            new MediaJob
            {
                FileFormat = FileFormatType.Jpg,
                Derivative = "medium",
                ImageTransform = image =>
                {
                    image.Quality = 85;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(0, 365));

                    return image;
                }
            },
            new MediaJob
            {
                FileFormat = FileFormatType.Jpg,
                Derivative = "large",
                ImageTransform = image =>
                {
                    image.Quality = 85;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(1600));

                    return image;
                }
            }
        };
    }
}