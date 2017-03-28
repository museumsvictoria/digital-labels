using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Extensions;
using DigitalLabels.Import.Infrastructure;
using DigitalLabels.Import.Utilities;
using ImageMagick;
using IMu;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Factories
{
    public class StandingStrongLabelImportFactory : ImportFactory<StandingStrongLabel>
    {
        private readonly IDocumentStore store;
        private Terms terms;

        public StandingStrongLabelImportFactory(IDocumentStore store)
        {
            this.store = store;
        }

        public override string ModuleName => "enarratives";

        public override string[] Columns => new[]
        {
            "irn",
            "NarTitle",
            "NarNarrative",
            "interviews=[name=IntIntervieweeRef_tab.(NamFirst,NamLast),IntInterviewLocation_tab,IntInterviewDate0,IntInterviewNotes_tab]",
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

                terms.Add("DetPurpose_tab", "Exhibition - Bunjilaka Standing Strong Digital Label");
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

        public override StandingStrongLabel Make(Map map)
        {
            var label = new StandingStrongLabel
            {
                Id = "standingstronglabels/" + map.GetString("irn"),
                Irn = long.Parse(map.GetString("irn")),
                DateModified = DateTime.ParseExact($"{map.GetString("AdmDateModified")} {map.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")), Story = map.GetString("NarNarrative")
            };

            // Quote
            var interview = map.GetMaps("interviews").FirstOrDefault();
            if (interview != null)
            {
                var intervieweeMap = interview.GetMap("name");
                var interviewee = string.Empty;
                if (intervieweeMap != null)
                {
                    interviewee = $"{intervieweeMap.GetString("NamFirst")} {intervieweeMap.GetString("NamLast")}".Trim();
                }

                label.Quote = interview.GetString("IntInterviewNotes_tab");

                label.QuoteSource = new[]
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
                if (media != null &&
                    string.Equals(media.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                    media.GetStrings("MdaDataSets_tab").Contains("Bunjilaka Digital Label"))
                {
                    var irn = long.Parse(media.GetString("irn"));
                    var elements = media.GetStrings("MdaElement_tab");
                    var freeTexts = media.GetStrings("MdaFreeText_tab");
                    var repositorys = media.GetStrings("ChaRepository_tab");
                    var dateModified = DateTime.ParseExact($"{media.GetString("AdmDateModified")} {media.GetString("AdmTimeModified")}", "dd/MM/yyyy HH:mm", new CultureInfo("en-AU"));

                    var length = Arrays.FindLongestLength(elements, freeTexts);

                    string description = string.Empty, photographer = string.Empty, source = string.Empty;

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
                            case "Image Order":
                                int order;
                                if (int.TryParse(freeText, out order))
                                    label.Order = order;
                                break;
                        }
                    }

                    var url = PathHelper.GetUrlPath(irn, FileFormatType.Jpg);

                    // Now we work out what the media is
                    if (repositorys != null && repositorys.Any(x => x == "Indigenous Online Images Square"))
                    {
                        if (MediaHelper.TrySaveStandingStrongThumbnail(irn, FileFormatType.Jpg, out var thumbnailType))
                        {
                            label.Thumbnail = new StandingStrongThumbnail
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
                        if (MediaHelper.TrySaveMedia(irn, imageMediaJob))
                        {
                            label.Image = new StandingStrongImage
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

            Log.Logger.Debug("Completed {id} creation", label.Id);

            return label;
        }

        private readonly MediaJob imageMediaJob = new MediaJob
        {
            FileFormat = FileFormatType.Jpg,
            ImageTransform = image =>
            {
                image.Quality = 85;
                image.Format = MagickFormat.Jpeg;
                image.Resize(new MagickGeometry(2000));

                return image;
            }
        };
    }
}
