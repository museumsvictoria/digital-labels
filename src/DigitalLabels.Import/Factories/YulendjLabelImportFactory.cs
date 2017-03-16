using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Utilities;
using ImageMagick;
using IMu;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Factories
{
    public class YulendjLabelImportFactory : ImportFactory<YulendjLabel>
    {
        private readonly IDocumentStore store;
        private Terms terms;

        public YulendjLabelImportFactory(IDocumentStore store)
        {
            this.store = store;
        }

        public override string ModuleName => "enarratives";

        public override string[] Columns => new[]
        {
            "irn",
            "NarTitle",
            "NarNarrative",
            "IntInterviewNotes_tab",
            "lastname=IntIntervieweeRef_tab.(NamLast)",
            "media=MulMultiMediaRef_tab.(irn,MulTitle,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
            "AdmDateModified",
            "AdmTimeModified"
        };

        public override Terms Terms
        {
            get
            {
                if (terms != null)
                    return terms;

                terms = new Terms();

                terms.Add("DetPurpose_tab", "Exhibition - Bunjilaka Yulendj Biographies Digital Label");
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

        public override YulendjLabel Make(Map map)
        {
            var label = new YulendjLabel
            {
                Id = "yulendjlabels/" + map.GetString("irn"),
                Irn = long.Parse(map.GetString("irn")),
                DateModified = DateTime.ParseExact($"{map.GetString("AdmDateModified")} {map.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")),
                Name = map.GetString("NarTitle"),
                Biography = map.GetString("NarNarrative"),
                Quote = map.GetStrings("IntInterviewNotes_tab").FirstOrDefault(),
                Order = map.GetMaps("lastname").FirstOrDefault()?.GetString("NamLast")
            };

            // Media
            var medias = map.GetMaps("media");
            foreach (var media in medias)
            {
                if (media != null &&
                    string.Equals(media.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                    media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                {
                    var irn = long.Parse(media.GetString("irn"));
                    var elements = media.GetStrings("MdaElement_tab");
                    var freeTexts = media.GetStrings("MdaFreeText_tab");
                    var dateModified = DateTime.ParseExact($"{media.GetString("AdmDateModified")} {media.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                    var length = Arrays.FindLongestLength(elements, freeTexts);

                    string description = string.Empty, photographer = string.Empty, source = string.Empty, imageType = string.Empty;

                    for (var i = 0; i < length; i++)
                    {
                        var element = i < elements.Length ? elements[i] : null;
                        var freeText = i < freeTexts.Length ? freeTexts[i] : null;

                        switch (element)
                        {
                            case "dcTitle":
                                description = freeText;
                                break;
                            case "Creator/Photographer":
                                photographer = freeText;
                                break;
                            case "dcSource":
                                source = freeText;
                                break;
                            case "Image Type":
                                imageType = freeText?.ToLower();
                                break;
                        }
                    }

                    var url = PathHelper.GetUrlPath(irn, FileFormatType.Jpg);
                    var image = new YulendjImage
                    {
                        DateModified = dateModified,
                        Description = description,
                        Irn = irn,
                        Photographer = photographer,
                        Source = source,
                        Url = url
                    };

                    // Now we work out what the media is
                    switch (imageType)
                    {
                        case "portrait":
                            if (MediaHelper.TrySaveMedia(irn, FileFormatType.Jpg, ImageTransforms["profileImage"]))
                                label.ProfileImage = image;
                            break;
                        case "texture":
                            if (MediaHelper.TrySaveMedia(irn, FileFormatType.Jpg, ImageTransforms["texturePanelImage"]))
                                label.TexturePanelImage = image;
                            break;
                    }
                }
            }

            Log.Logger.Debug("Completed {id} creation", label.Id);

            return label;
        }

        private readonly Dictionary<string, Func<MagickImage, MagickImage>> ImageTransforms = new Dictionary<string, Func<MagickImage, MagickImage>>
        {
            {
                "profileImage",
                image =>
                {
                    image.Quality = 90;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(500, 750));

                    return image;
                }
            },
            {
                "texturePanelImage",
                image =>
                {
                    image.Quality = 85;
                    image.Format = MagickFormat.Jpeg;
                    image.Resize(new MagickGeometry(750));

                    return image;
                }
            }
        };
    }
}
