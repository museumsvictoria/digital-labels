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
    public static class YulendjLabelFactory
    {
        public static YulendjLabel MakeLabel(Map map)
        {
            var newLabel = new YulendjLabel();

            // Irn & DateModified
            newLabel.Id = "yulendjlabels/" + map.GetString("irn");
            newLabel.Irn = long.Parse(map.GetString("irn"));
            newLabel.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"));

            // Narrative 
            newLabel.Name = map.GetString("NarTitle");
            newLabel.Biography = map.GetString("NarNarrative");
            newLabel.Quote = map.GetStrings("IntInterviewNotes_tab").FirstOrDefault();

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

                    string description = string.Empty, photographer = string.Empty, source = string.Empty, imageType = string.Empty;

                    for (var i = 0; i < length; i++)
                    {
                        var element = (i < elements.Length) ? elements[i] : null;
                        var freeText = (i < freeTexts.Length) ? freeTexts[i] : null;

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
                                imageType = freeText.ToLower();
                                break;
                        }
                    }

                    var resizeSettings = new ResizeSettings
                        {
                            Format = FileFormatType.Jpg.ToString(),
                            MaxHeight = 750,
                            MaxWidth = 500,
                            Quality = 90
                        };
                    var url = PathFactory.GetUrlPath(irn, FileFormatType.Jpg);
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
                    if (imageType == "portrait")
                    {
                        if (MediaSaver.Save(fileStream, irn, FileFormatType.Jpg, resizeSettings))
                        {
                            newLabel.ProfileImage = image;
                        }
                    }
                    else if (imageType == "texture")
                    {
                        if (MediaSaver.Save(fileStream, irn, FileFormatType.Jpg, resizeSettings))
                        {
                            newLabel.TexturePanelImage = image;
                        }
                    }
                }
            }

            return newLabel;
        }
    }
}