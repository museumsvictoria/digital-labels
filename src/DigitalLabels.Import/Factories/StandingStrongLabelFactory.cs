using System;
using System.Globalization;
using System.IO;
using System.Linq;
using DigitalLabels.Core.DomainModels;
using IMu;
using DigitalLabels.Import.Utilities;
using ImageResizer;
using DigitalLabels.Core.Extensions;

namespace DigitalLabels.Import.Factories
{
    public static class StandingStrongLabelFactory
    {
        public static StandingStrongLabel MakeLabel(Map map)
        {
            var newLabel = new StandingStrongLabel();

            // Irn & DateModified
            newLabel.Id = "standingstronglabels/" + map.GetString("irn");
            newLabel.Irn = long.Parse(map.GetString("irn"));
            newLabel.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"));

            // Narrative
            newLabel.Story = map.GetString("NarNarrative");
            
            // Quote
            var interview = map.GetMaps("interviews").FirstOrDefault();
            if (interview != null)
            {
                var intervieweeMap = interview.GetMap("name");
                var interviewee = string.Empty;
                if (intervieweeMap != null)
                {
                    interviewee = intervieweeMap.GetString("NamFullName");
                }

                newLabel.Quote = interview.GetString("IntInterviewNotes_tab");

                newLabel.QuoteSource = new[]
                    {
                        interviewee,
                        interview.GetString("IntInterviewLocation_tab"),
                        interview.GetString("IntInterviewDate0")
                    }.Concatenate(", ");
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

                    string description = string.Empty, photographer = string.Empty, source = string.Empty;

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
                            case "Image Order":
                                int order;
                                if (int.TryParse(freeText, out order))
                                    newLabel.Order = order;
                                break;
                        }
                    }

                    var url = PathFactory.GetUrlPath(irn, FileFormatType.Jpg);

                    // Now we work out what the media is
                    if (repositorys != null && repositorys.Any(x => x == "Indigenous Online Images Square"))
                    {
                        var thumbnailType = MediaHelper.GetStandingStrongThumbnailType(fileStream);

                        if (MediaHelper.Save(fileStream, irn, FileFormatType.Jpg, null))
                        {
                            newLabel.Thumbnail = new StandingStrongThumbnail()
                                {
                                    Irn = irn,
                                    DateModified = dateModified,
                                    Url = url,
                                    Type = thumbnailType.ToString()
                                };
                        }
                    }
                    else
                    {                        
                        var resizeSettings = new ResizeSettings
                            {
                                Format = FileFormatType.Jpg.ToString(),
                                MaxHeight = 2000,
                                MaxWidth = 2000,
                                Quality = 90
                            };

                        if (MediaHelper.Save(fileStream, irn, FileFormatType.Jpg, resizeSettings))
                        {
                            newLabel.Image = new StandingStrongImage()
                                {
                                    DateModified = dateModified,
                                    Description = description,
                                    Irn = irn,
                                    Photographer = photographer,
                                    Source = source,
                                    Url = url
                                };
                        }
                    }
                }
            }

            return newLabel;
        }
    }
}