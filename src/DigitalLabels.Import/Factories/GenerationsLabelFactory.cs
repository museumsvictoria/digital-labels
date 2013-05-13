using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DigitalLabels.Core.DomainModels;
using IMu;
using DigitalLabels.Import.Utilities;
using ImageResizer;

namespace DigitalLabels.Import.Factories
{
    public static class GenerationsLabelFactory
    {
        public static GenerationsQuote MakeQuote(Map map)
        {
            var newQuote = new GenerationsQuote();

            // Irn
            newQuote.Irn = long.Parse(map.GetString("irn"));
            newQuote.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"));

            // Reg No.
            if (map.GetString("ColRegPart") != null)
            {
                newQuote.RegistrationNumber = string.Format("{0}{1}.{2}", map.GetString("ColRegPrefix"), map.GetString("ColRegNumber"), map.GetString("ColRegPart"));
            }
            else
            {
                newQuote.RegistrationNumber = string.Format("{0}{1}", map.GetString("ColRegPrefix"), map.GetString("ColRegNumber"));
            }

            // Caption/HeaderText
            foreach (var caption in map.GetMaps("captions"))
            {
                switch (caption.GetString("DesPurpose_tab"))
                {
                    case "Bunjilaka Header Text 2012":
                        newQuote.HeaderText = caption.GetString("DesCaption_tab");
                        break;
                    case "Bunjilaka Caption 2012":
                        newQuote.Caption = caption.GetString("DesCaption_tab");
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
                            if (languageGroup.GetString("NamPartyType") == "Person")
                                newQuote.LanguageGroup = languageGroup.GetStrings("NamOtherNames_tab").FirstOrDefault();
                            else
                                newQuote.LanguageGroup = languageGroup.GetString("NamOrganisation");
                        }
                        break;
                    case "Place Depicted":
                        var locality = association.GetString("AssAssociationLocality_tab");
                        var state = association.GetString("AssAssociationState_tab");
                        newQuote.Place = string.Format("{0} {1}", locality, state);
                        break;
                    case "Date Depicted":
                        newQuote.Date = association.GetString("AssAssociationDate_tab");
                        break;
                    case "Photographer":
                        var photographer = association.GetMap("name");
                        if (photographer != null)
                        {
                            if (photographer.GetString("NamPartyType") == "Person")
                                newQuote.Photographer = photographer.GetStrings("NamOtherNames_tab").FirstOrDefault();
                            else
                                newQuote.Photographer = photographer.GetString("NamOrganisation");
                        }
                        break;
                }
            }

            // Media
            var medias = map.GetMaps("media");
            foreach (var media in medias)
            {
                if (media.GetString("AdmPublishWebNoPassword") == "Yes" && media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                {
                    var irn = long.Parse(media.GetString("irn"));
                    var type = media.GetString("MulMimeType");
                    var fileStream = media.GetMap("resource")["file"] as FileStream;
                    var elements = media.GetStrings("MdaElement_tab");
                    var freeTexts = media.GetStrings("MdaFreeText_tab");
                    var repositorys = media.GetStrings("ChaRepository_tab");
                    var dateModified = DateTime.ParseExact(
                        string.Format("{0} {1}", media.GetString("AdmDateModified"), media.GetString("AdmTimeModified")),
                        "dd/MM/yyyy HH:mm",
                        new CultureInfo("en-AU"));

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
                    if (repositorys != null && repositorys.Any(x => x == "Indigenous Online Images Square"))
                    {
                        var url = PathFactory.GetUrlPath(irn, FileFormatType.Jpg);
                        var resizeSettings = new ResizeSettings
                        {
                            Format = FileFormatType.Jpg.ToString(),
                            MaxHeight = 105,
                            MaxWidth = 105,
                            Quality = 90
                        };

                        if (MediaSaver.Save(fileStream, irn, FileFormatType.Jpg, resizeSettings))
                        {
                            newQuote.Thumbnail = new MediaAsset
                            {
                                Irn = irn,
                                DateModified = dateModified,
                                Url = url
                            };
                        }
                    }
                    else if (type == "image")
                    {
                        var mediumUrl = PathFactory.GetUrlPath(irn, FileFormatType.Jpg, "medium");
                        var largeUrl = PathFactory.GetUrlPath(irn, FileFormatType.Jpg, "large");
                        var mediumResizeSettings = new ResizeSettings
                        {
                            Format = FileFormatType.Jpg.ToString(),
                            Height = 365,
                            Mode = FitMode.Max,
                            Quality = 85
                        };
                        var largeResizeSettings = new ResizeSettings
                        {
                            Format = FileFormatType.Jpg.ToString(),
                            MaxHeight = 1600,
                            MaxWidth = 1600,
                            Quality = 85
                        };

                        if (MediaSaver.Save(fileStream, irn, FileFormatType.Jpg, mediumResizeSettings, "medium", true) &&
                            MediaSaver.Save(fileStream, irn, FileFormatType.Jpg, largeResizeSettings, "large"))
                        {
                            newQuote.Image = new GenerationsImage
                            {
                                Acknowledgements = acknowledgements,
                                DateModified = dateModified,
                                Irn = irn,
                                LargeUrl = largeUrl,
                                MediumUrl = mediumUrl,
                                Order = order,
                                Source = source
                            };
                        }
                    }
                }
            }

            // Narrative
            var narrative = map.GetMaps("narrative")
                .FirstOrDefault(x => x.GetStrings("DetPurpose_tab").Any(y => y.Contains("Exhibition - Bunjilaka Generations Digital Label")) && x.GetString("AdmPublishWebNoPassword") == "Yes");

            if (narrative != null)
            {
                var dateTime = DateTime.ParseExact(
                    string.Format("{0} {1}", narrative.GetString("AdmDateModified"), narrative.GetString("AdmTimeModified")),
                    "dd/MM/yyyy HH:mm",
                    new CultureInfo("en-AU"));

                if (dateTime > newQuote.DateModified)
                    newQuote.DateModified = dateTime;

                newQuote.NarrativeIrn = long.Parse(narrative.GetString("irn"));

                var masterNarrative = narrative.GetMap("master");
                if (masterNarrative != null && !string.IsNullOrWhiteSpace(masterNarrative.GetString("irn")))
                    newQuote.PrimaryImageNarrativeIrn = long.Parse(masterNarrative.GetString("irn"));

                newQuote.PeopleDepicted = narrative.GetStrings("DepPeople0").FirstOrDefault();
                newQuote.QuoteDescription = narrative.GetString("NarNarrativeSummary");
                newQuote.Quote = HtmlConverter.HtmlToText(narrative.GetString("NarNarrative"));

                var interviews = narrative.GetMaps("interviews");

                foreach (var interview in interviews)
                {
                    newQuote.QuoteSource = interview.GetString("IntInterviewLocation_tab");

                    var interviewer = interview.GetMap("name");
                    if (interviewer != null)
                    {
                        if (interviewer.GetString("NamPartyType") == "Person")
                            newQuote.QuoteAuthor = interviewer.GetStrings("NamOtherNames_tab").FirstOrDefault();
                        else
                            newQuote.QuoteAuthor = interviewer.GetString("NamOrganisation");
                    }
                }
            }

            return newQuote;
        }

        public static GenerationsLabel MakeLabel(Map map)
        {
            var newLabel = new GenerationsLabel
                {
                    Id = "generationslabels/" + map.GetString("irn"),
                    Theme = map.GetString("ClaTertiaryClassification"),
                    PrimaryQuote = MakeQuote(map)
                };

            return newLabel;
        }
    }
}
