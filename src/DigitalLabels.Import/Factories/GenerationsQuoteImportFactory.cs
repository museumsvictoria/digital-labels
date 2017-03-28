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
    public class GenerationsQuoteImportFactory : ImportFactory<GenerationsQuote>
    {
        private readonly IDocumentStore store;
        private Terms terms;

        public GenerationsQuoteImportFactory(IDocumentStore store)
        {
            this.store = store;
        }

        public override string ModuleName => "enarratives";

        public override string[] Columns => new[]
        {
            "irn",
            "DetPurpose_tab",
            "DepPeople0",
            "NarNarrative",
            "interviews=[name=IntIntervieweeRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),IntInterviewLocation_tab]",
            "master=AssMasterNarrativeRef.irn",
            "AdmDateModified",
            "AdmTimeModified",
            "media=MulMultiMediaRef_tab.(irn,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
            "catalog=ObjObjectsRef_tab.(irn,ColRegPrefix,ColRegNumber,ColRegPart,ClaObjectName,ClaTertiaryClassification,AdmDateModified,AdmTimeModified,captions=[DesCaption_tab,DesPurpose_tab],associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab])"
        };

        public override Terms Terms
        {
            get
            {
                if (terms != null)
                    return terms;

                terms = new Terms();

                terms.Add("DetPurpose_tab", "Exhibition - Bunjilaka Generations Digital Label");
                terms.Add("NarTitle", "supporting");
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

        public override GenerationsQuote Make(Map map)
        {
            var label = new GenerationsQuote
            {
                Irn = long.Parse(map.GetString("irn")),
                DateModified = DateTime.ParseExact($"{map.GetString("AdmDateModified")} {map.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")),
                PeopleDepicted = map.GetStrings("DepPeople0").FirstOrDefault(),
                Quote = HtmlConverter.HtmlToText(map.GetString("NarNarrative"))
            };

            var interviews = map.GetMaps("interviews");

            foreach (var interview in interviews)
            {
                label.QuoteSource = interview.GetString("IntInterviewLocation_tab");

                var interviewer = interview.GetMap("name");
                if (interviewer != null)
                {
                    label.QuoteAuthor = interview.GetMap("name")?.GetString("NamPartyType") == "Person" ? interviewer.GetStrings("NamOtherNames_tab").FirstOrDefault() : interviewer.GetString("NamOrganisation");
                }
            }

            var masterNarrative = map.GetMap("master");
            if (!string.IsNullOrWhiteSpace(masterNarrative?.GetString("irn")))
                label.PrimaryImageNarrativeIrn = long.Parse(masterNarrative.GetString("irn"));

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
                    var repositorys = media.GetStrings("ChaRepository_tab");
                    var dateModified = DateTime.ParseExact($"{media.GetString("AdmDateModified")} {media.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                    var length = Arrays.FindLongestLength(elements, freeTexts);

                    string source = string.Empty, acknowledgements = string.Empty, order = string.Empty;

                    for (int i = 0; i < length; i++)
                    {
                        var element = (i < elements.Length) ? elements[i] : null;
                        var freeText = (i < freeTexts.Length) ? freeTexts[i] : null;

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

                    // Now we work out what the media is
                    if (repositorys != null && repositorys.All(x => x != "Indigenous Online Images Square") && type == "image")
                    {
                        if (MediaHelper.TrySaveMedia(irn, imageMediaJobs))
                        {
                            label.Image = new GenerationsImage
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
            }

            // Catalog
            var catalog = map.GetMaps("catalog").FirstOrDefault(x => x.GetString("ClaObjectName").ToLower().Contains("supporting"));

            if (catalog != null)
            {
                // Reg No.
                label.RegistrationNumber = catalog.GetString("ColRegPart") != null ? $"{catalog.GetString("ColRegPrefix")}{catalog.GetString("ColRegNumber")}.{catalog.GetString("ColRegPart")}" : $"{catalog.GetString("ColRegPrefix")}{catalog.GetString("ColRegNumber")}";

                // Caption/HeaderText
                foreach (var caption in catalog.GetMaps("captions"))
                {
                    switch (caption.GetString("DesPurpose_tab"))
                    {
                        case "Bunjilaka Header Text 2012":
                            label.HeaderText = caption.GetString("DesCaption_tab");
                            break;
                        case "Bunjilaka Caption 2012":
                            label.Caption = caption.GetString("DesCaption_tab");
                            break;
                    }
                }

                // Associations
                foreach (var association in catalog.GetMaps("associations"))
                {
                    switch (association.GetString("AssAssociationType_tab"))
                    {
                        case "Clan/Language Group":
                            var languageGroup = association.GetMap("name");
                            if (languageGroup != null)
                            {
                                label.LanguageGroup = languageGroup.GetString("NamPartyType") == "Person" ? languageGroup.GetStrings("NamOtherNames_tab").FirstOrDefault() : languageGroup.GetString("NamOrganisation");
                            }
                            break;
                        case "Place Depicted":
                            label.Place = $"{association.GetString("AssAssociationLocality_tab")} {association.GetString("AssAssociationState_tab")}";
                            break;
                        case "Date Depicted":
                            label.Date = association.GetString("AssAssociationDate_tab");
                            break;
                        case "Photographer":
                            var photographer = association.GetMap("name");
                            if (photographer != null)
                            {
                                label.Photographer = photographer.GetString("NamPartyType") == "Person" ? photographer.GetStrings("NamOtherNames_tab").FirstOrDefault() : photographer.GetString("NamOrganisation");
                            }
                            break;
                    }
                }
            }

            Log.Logger.Debug("Completed generations quote {id} creation", label.Irn);

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