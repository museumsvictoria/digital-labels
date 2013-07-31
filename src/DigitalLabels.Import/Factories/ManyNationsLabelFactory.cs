using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DigitalLabels.Core.DomainModels;
using IMu;
using ImageResizer;
using NLog;
using DigitalLabels.Import.Utilities;
using DigitalLabels.Core.Extensions;

namespace DigitalLabels.Import.Factories
{
    public static class ManyNationsLabelFactory
    {
        public static ManyNationsLabel MakeLabel(Map map)
        {
            var newLabel = new ManyNationsLabel();

            // Irn/Id/DateModified
            newLabel.Id = "manynationslabels/" + map.GetString("irn");
            newLabel.Irn = long.Parse(map.GetString("irn"));
            newLabel.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"));

            // Reg No.
            if (map["ColRegPart"] != null)
            {
                newLabel.RegistrationNumber = string.Format("{0}{1}.{2}", map["ColRegPrefix"], map["ColRegNumber"], map["ColRegPart"]);
            }
            else
            {
                newLabel.RegistrationNumber = string.Format("{0}{1}", map["ColRegPrefix"], map["ColRegNumber"]);
            }

            // Common Name
            newLabel.CommonName = map.GetString("ClaObjectName");

            // Language Name
            if (!string.IsNullOrWhiteSpace(map.GetString("DesLocalName")))
                newLabel.LanguageName = map.GetString("DesLocalName").Replace("(Bunjilaka Project 2012)", "").Trim();

            // Associations
            foreach (var association in map.GetMaps("associations"))
            {
                switch (association.GetString("AssAssociationType_tab"))
                {
                    case "Date Made":
                        newLabel.DateMade = association.GetString("AssAssociationDate_tab");
                        break;
                    case "Maker":
                        var maker = association.GetMap("name");
                        if (maker != null)
                        {
                            if (maker.GetString("NamPartyType") == "Person")
                                newLabel.Maker = maker.GetStrings("NamOtherNames_tab").FirstOrDefault();
                            else
                                newLabel.Maker = maker.GetString("NamOrganisation");
                        }
                        break;
                    case "Clan/Language Group":
                        var languageGroup = association.GetMap("name");
                        if (languageGroup != null)
                        {
                            if (languageGroup.GetString("NamPartyType") == "Person")
                                newLabel.LanguageGroup = languageGroup.GetStrings("NamOtherNames_tab").FirstOrDefault();
                            else
                                newLabel.LanguageGroup = languageGroup.GetString("NamOrganisation");
                        }
                        break;
                    case "Place Made":
                        var locality = association.GetString("AssAssociationLocality_tab");
                        var state = association.GetString("AssAssociationState_tab");
                        newLabel.PlaceMade = new[]{ locality, state }.Concatenate(", ");
                        // Also state.
                        if(!string.IsNullOrWhiteSpace(state))
                            newLabel.State = state;
                        break;
                    case "Indigenous Region":
                        newLabel.Region = association.GetString("AssAssociationRegion_tab");
                        break;
                    case "Bunjilaka Material 2012":
                        var material = association.GetString("MatTertiaryMaterials_tab");
                        newLabel.Materials = (newLabel.Materials == null) ? material : string.Format("{0}, {1}", newLabel.Materials, material);
                        break;
                }
            }

            // Materials
            foreach (var material in map.GetMaps("materials"))
            {
                var primary = material.GetString("MatPrimaryMaterials_tab");
                var tertiary = material.GetString("MatTertiaryMaterials_tab");

                if(primary.Contains("Bunjilaka Material"))
                {
                    newLabel.Materials = (newLabel.Materials == null) ? tertiary : newLabel.Materials + ", " + tertiary;
                }
            }

            // Exhibition Objects
            var collectionObject = map.GetMaps("collobjs").FirstOrDefault(x => x.GetString("StaGridCode") == "First People - Many Nations");
            if(collectionObject != null)
            {
                newLabel.Case = collectionObject.GetString("StaCase");
                newLabel.Segment = collectionObject.GetString("StaSegmentName").Replace(@"Many Nations - ", "").Trim();
            }
            
            // Narrative
            var narrative = map.GetMaps("narrative")
                .FirstOrDefault(x => x.GetStrings("DetPurpose_tab").Any(y => y.Contains("Bunjilaka Many Nations Digital Label")) && x.GetString("AdmPublishWebNoPassword") == "Yes");

            if (narrative != null)
            {
                var dateTime = DateTime.ParseExact(
                    string.Format("{0} {1}", narrative.GetString("AdmDateModified"), narrative.GetString("AdmTimeModified")),
                    "dd/MM/yyyy HH:mm",
                    new CultureInfo("en-AU"));

                if (dateTime > newLabel.DateModified)
                    newLabel.DateModified = dateTime;

                // Convert Html to Text
                newLabel.Story = HtmlConverter.HtmlToText(narrative.GetString("NarNarrative"));

                // Author(s)
                var authors = narrative.GetMaps("name");
                if (authors != null)
                {
                    newLabel.StoryAuthor = authors.Select(x => x.GetString("NamFullName")).Concatenate(", ");
                }

                // Media
                var medias = narrative.GetMaps("media");
                newLabel.Images = new List<ManyNationsImage>();
                foreach (var media in medias)
                {
                    if (media != null && media.GetString("AdmPublishWebNoPassword") == "Yes" && media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                    {
                        var irn = long.Parse(media.GetString("irn"));
                        var type = media.GetString("MulMimeType");
                        var fileStream = media.GetMap("resource")["file"] as FileStream;
                        var elements = media.GetStrings("MdaElement_tab");
                        var qualifiers = media.GetStrings("MdaQualifier_tab");
                        var freeTexts = media.GetStrings("MdaFreeText_tab");
                        var repositorys = media.GetStrings("ChaRepository_tab");
                        var dateModified = DateTime.ParseExact(
                            string.Format("{0} {1}", media.GetString("AdmDateModified"), media.GetString("AdmTimeModified")),
                            "dd/MM/yyyy HH:mm",
                            new CultureInfo("en-AU"));
                        var title = media.GetString("MulTitle");

                        var length = Arrays.FindLongestLength(elements, qualifiers, freeTexts);

                        string creator = string.Empty, description = string.Empty, source = string.Empty, copyrightHolder = string.Empty, order = string.Empty, imageType = string.Empty;

                        for (var i = 0; i < length; i++)
                        {
                            var element = (i < elements.Length) ? elements[i] : null;
                            var qualifier = (i < qualifiers.Length) ? qualifiers[i] : null;
                            var freeText = (i < freeTexts.Length) ? freeTexts[i] : null;

                            switch (element)
                            {
                                case "Creator/Photographer":
                                    creator = freeText;
                                    break;
                                case "dcTitle":
                                    description = freeText;
                                    break;
                                case "dcSource":
                                    source = freeText;
                                    break;
                                case "dcRights":
                                    copyrightHolder = freeText;
                                    break;
                                case "Image Order":
                                    order = freeText;
                                    if (!string.IsNullOrWhiteSpace(freeText))
                                        imageType = freeText.Split(' ').FirstOrDefault();
                                    break;
                            }
                        }

                        // Now we work out what the media is
                        if (repositorys != null && repositorys.Any(x => x == "ICD Online Images Map"))
                        {
                            newLabel.MapReference = title;
                        }
                        else if (repositorys != null && repositorys.Any(x => x == "Indigenous Online Images Square"))
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
                                newLabel.Thumbnail = new MediaAsset
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
                                Width = 649,
                                Mode = FitMode.Pad,
                                PaddingColor = Color.White,
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
                                newLabel.Images.Add(new ManyNationsImage
                                {
                                    CopyrightHolder = copyrightHolder,
                                    Creator = creator,
                                    Description = description,
                                    Irn = irn,
                                    Order = order,
                                    Type = imageType,
                                    Source = source,
                                    DateModified = dateModified,
                                    MediumUrl = mediumUrl,
                                    LargeUrl = largeUrl
                                });
                            }
                        }
                        else if (type == "video")
                        {
                            var url = PathFactory.GetUrlPath(irn, FileFormatType.Mp4);
                            if (MediaSaver.Save(fileStream, irn, FileFormatType.Mp4, null))
                            {
                                newLabel.Video = new ManyNationsVideo
                                    {
                                        CopyrightHolder = copyrightHolder,
                                        Creator = creator,
                                        Description = description,
                                        Irn = irn,
                                        Order = order,
                                        Source = source,
                                        DateModified = dateModified,
                                        Url = url
                                    };
                            }
                        }
                    }
                }
            }

            return newLabel;
        }
    }
}
