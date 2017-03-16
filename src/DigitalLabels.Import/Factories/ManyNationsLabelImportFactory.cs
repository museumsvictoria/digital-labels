using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Extensions;
using DigitalLabels.Import.Utilities;
using ImageMagick;
using IMu;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Factories
{
    public class ManyNationsLabelImportFactory : ImportFactory<ManyNationsLabel>
    {
        private readonly IDocumentStore store;
        private Terms terms;

        public ManyNationsLabelImportFactory(IDocumentStore store)
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
            "ClaObjectName",
            "DesLocalName",
            "AdmDateModified",
            "AdmTimeModified",
            "materials=[MatPrimaryMaterials_tab,MatTertiaryMaterials_tab]",
            "associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab,AssAssociationRegion_tab,MatTertiaryMaterials_tab]",
            "narrative=<enarratives:ObjObjectsRef_tab>.(DetPurpose_tab,name=NarAuthorsRef_tab.NamFullName,NarNarrative,media=MulMultiMediaRef_tab.(irn,MulTitle,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified),AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
            "collobjs=<eexhibitobjects:StaObjectRef>.(irn,StaGridCode,StaSegmentName,StaCase)"
        };

        public override Terms Terms
        {
            get
            {
                if (terms != null)
                    return terms;

                terms = new Terms();

                terms.Add("MdaDataSets_tab", "Bunjilaka Digital Label");
                terms.Add("ClaSecondaryClassification", "Many Nations");
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

        public override ManyNationsLabel Make(Map map)
        {
            var label = new ManyNationsLabel
            {
                Id = "manynationslabels/" + map.GetString("irn"),
                Irn = long.Parse(map.GetString("irn")),
                DateModified = DateTime.ParseExact($"{map.GetString("AdmDateModified")} {map.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")),
                RegistrationNumber = map["ColRegPart"] != null ? $"{map["ColRegPrefix"]}{map["ColRegNumber"]}.{map["ColRegPart"]}" : $"{map["ColRegPrefix"]}{map["ColRegNumber"]}",
                CommonName = map.GetString("ClaObjectName")
            };

            // Language Name
            if (!string.IsNullOrWhiteSpace(map.GetString("DesLocalName")))
                label.LanguageName = HtmlConverter.HtmlToText(map.GetString("DesLocalName").Replace("(Bunjilaka Project 2012)", "").Trim());

            // Associations
            foreach (var association in map.GetMaps("associations"))
            {
                switch (association.GetString("AssAssociationType_tab"))
                {
                    case "Date Made":
                        label.DateMade = association.GetString("AssAssociationDate_tab");
                        break;
                    case "Maker":
                        var maker = association.GetMap("name");
                        if (maker != null)
                        {
                            label.Maker = maker.GetString("NamPartyType") == "Person" ? maker.GetStrings("NamOtherNames_tab").FirstOrDefault() : maker.GetString("NamOrganisation");
                        }
                        break;
                    case "Clan/Language Group":
                        var languageGroup = association.GetMap("name");
                        if (languageGroup != null)
                        {
                            label.LanguageGroup = languageGroup.GetString("NamPartyType") == "Person" ? languageGroup.GetStrings("NamOtherNames_tab").FirstOrDefault() : languageGroup.GetString("NamOrganisation");
                        }
                        break;
                    case "Place Made":
                        var locality = association.GetString("AssAssociationLocality_tab");
                        var state = association.GetString("AssAssociationState_tab");
                        label.PlaceMade = new[] { locality, state }.Concatenate(", ");
                        // Also state.
                        if (!string.IsNullOrWhiteSpace(state))
                            label.State = state;
                        break;
                    case "Indigenous Region":
                        label.Region = association.GetString("AssAssociationRegion_tab");
                        break;
                    case "Bunjilaka Material 2012":
                        var material = association.GetString("MatTertiaryMaterials_tab");
                        label.Materials = (label.Materials == null) ? material : $"{label.Materials}, {material}";
                        break;
                }
            }

            // Materials
            foreach (var material in map.GetMaps("materials"))
            {
                var primary = material.GetString("MatPrimaryMaterials_tab");
                var tertiary = material.GetString("MatTertiaryMaterials_tab");

                if (primary != null && primary.Contains("Bunjilaka Material"))
                {
                    label.Materials = label.Materials == null ? tertiary : label.Materials + ", " + tertiary;
                }
            }

            // Exhibition Objects
            var collectionObject = map.GetMaps("collobjs").FirstOrDefault(x => x.GetString("StaGridCode") == "First People - Many Nations");
            if (collectionObject != null)
            {
                label.Case = collectionObject.GetString("StaCase");
                label.Segment = collectionObject.GetString("StaSegmentName").Replace(@"Many Nations - ", "").Trim();
            }

            // Narrative
            var narrative = map.GetMaps("narrative")
                .FirstOrDefault(x => x.GetStrings("DetPurpose_tab").Any(y => y.Contains("Bunjilaka Many Nations Digital Label")) && string.Equals(x.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase));

            if (narrative != null)
            {
                var dateTime = DateTime.ParseExact($"{narrative.GetString("AdmDateModified")} {narrative.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                if (dateTime > label.DateModified)
                    label.DateModified = dateTime;

                // Convert Html to Text
                label.Story = HtmlConverter.HtmlToText(narrative.GetString("NarNarrative"));

                // Author(s)
                var authors = narrative.GetMaps("name");
                if (authors != null)
                {
                    label.StoryAuthor = authors.Select(x => x.GetString("NamFullName")).Concatenate(", ");
                }

                // Media
                var medias = narrative.GetMaps("media");
                label.Images = new List<ManyNationsImage>();
                foreach (var media in medias)
                {
                    if (media != null &&
                        string.Equals(media.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                        media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                    {
                        var irn = long.Parse(media.GetString("irn"));
                        var type = media.GetString("MulMimeType");                        
                        var elements = media.GetStrings("MdaElement_tab");
                        var qualifiers = media.GetStrings("MdaQualifier_tab");
                        var freeTexts = media.GetStrings("MdaFreeText_tab");
                        var repositories = media.GetStrings("ChaRepository_tab");
                        var dateModified = DateTime.ParseExact($"{media.GetString("AdmDateModified")} {media.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));
                        var title = media.GetString("MulTitle");

                        var length = Arrays.FindLongestLength(elements, qualifiers, freeTexts);

                        string creator = string.Empty, description = string.Empty, source = string.Empty, copyrightHolder = string.Empty, order = string.Empty, imageType = string.Empty;

                        for (var i = 0; i < length; i++)
                        {
                            var element = i < elements.Length ? elements[i] : null;
                            var freeText = i < freeTexts.Length ? freeTexts[i] : null;

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
                        if (repositories != null && repositories.Any(x => x == "ICD Online Images Map"))
                        {
                            label.MapReference = title;
                        }
                        else if (repositories != null && repositories.Any(x => x == "Indigenous Online Images Square"))
                        {
                            if (MediaHelper.TrySaveMedia(irn, FileFormatType.Jpg, ImageTransforms["thumbnail"]))
                            {
                                label.Thumbnail = new MediaAsset
                                {
                                    Irn = irn,
                                    DateModified = dateModified,
                                    Url = PathHelper.GetUrlPath(irn, FileFormatType.Jpg)
                                };
                            }
                        }
                        else if (type == "image")
                        {
                            if (MediaHelper.TrySaveMedia(irn, FileFormatType.Jpg, ImageTransforms["medium"], "medium") &&
                                MediaHelper.TrySaveMedia(irn, FileFormatType.Jpg, ImageTransforms["large"], "large"))
                            {
                                label.Images.Add(new ManyNationsImage
                                {
                                    CopyrightHolder = copyrightHolder,
                                    Creator = creator,
                                    Description = description,
                                    Irn = irn,
                                    Order = order,
                                    Type = imageType,
                                    Source = source,
                                    DateModified = dateModified,
                                    MediumUrl = PathHelper.GetUrlPath(irn, FileFormatType.Jpg, "medium"),
                                    LargeUrl = PathHelper.GetUrlPath(irn, FileFormatType.Jpg, "large")
                                });
                            }
                        }
                        else if (type == "video")
                        {
                            if (MediaHelper.TrySaveMedia(irn, FileFormatType.Mp4))
                            {
                                label.Video = new ManyNationsVideo
                                {
                                    CopyrightHolder = copyrightHolder,
                                    Creator = creator,
                                    Description = description,
                                    Irn = irn,
                                    Order = order,
                                    Source = source,
                                    DateModified = dateModified,
                                    Url = PathHelper.GetUrlPath(irn, FileFormatType.Mp4)
                                };
                            }
                        }
                    }
                }
            }

            Log.Logger.Debug("Completed {id} creation", label.Id);

            return label;
        }

        private readonly Dictionary<string, Func<MagickImage, MagickImage>> ImageTransforms = new Dictionary<string, Func<MagickImage, MagickImage>>
        {
            {
                "thumbnail",
                image =>
                {
                    image.Quality = 90;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(105));
                    return image;
                }
            },
            {
                "medium",
                image =>
                {
                    image.Quality = 85;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(649, 365));
                    image.Extent(new MagickGeometry(649, 365), Gravity.Center, MagickColors.White);

                    return image;
                }
            },
            {
                "large",
                image =>
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
