using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using ImageMagick;
using IMu;
using Serilog;

namespace DigitalLabels.Import.Utilities
{
    public static class MediaHelper
    {
        public static bool TrySaveMedia(long irn, MediaJob mediaJob)
        {
            return TrySaveMedia(irn, new[] { mediaJob });
        }

        public static bool TrySaveMedia(long irn, IEnumerable<MediaJob> mediaJobs)
        {
            var stopwatch = Stopwatch.StartNew();

            // first see if we are checking for existing media
            if (bool.Parse(ConfigurationManager.AppSettings["CheckExistingMedia"]) && FileExists(irn, mediaJobs))
            {
                stopwatch.Stop();
                Log.Logger.Debug("Found existing image {Irn} in {ElapsedMilliseconds} ms", irn, stopwatch.ElapsedMilliseconds);

                return true;
            }

            try
            {
                using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");

                    using (var fileStream = resource["file"] as FileStream)
                    {
                        foreach (var mediaJob in mediaJobs)
                        {
                            using (var file = File.Open(PathHelper.MakeDestPath(irn, mediaJob.FileFormat, mediaJob.Derivative), FileMode.Create, FileAccess.ReadWrite))
                            {
                                // if image transform has been supplied apply transform if not simply write file to disk
                                if (mediaJob.ImageTransform != null)
                                {
                                    using (var image = mediaJob.ImageTransform(new MagickImage(fileStream)))
                                    {
                                        image.Write(file);
                                    }
                                }
                                else
                                {
                                    fileStream.CopyTo(file);
                                }

                                Log.Logger.Debug("Written file {fileName}", PathHelper.GetFileName(irn, mediaJob.FileFormat, mediaJob.Derivative));

                                fileStream.Position = 0;
                            }
                        }
                    }
                }

                stopwatch.Stop();

                Log.Logger.Debug("Completed media with irn {irn} creation in {ElapsedMilliseconds}ms", irn, stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error saving media with irn {irn}", irn);
            }

            return false;
        }
        
        public static bool TrySaveStandingStrongThumbnail(long irn, FileFormatType fileFormat, out StandingStrongThumbnailType? thumbnailType)
        {
            var stopwatch = Stopwatch.StartNew();

            // first see if we are checking for existing media
            if (bool.Parse(ConfigurationManager.AppSettings["CheckExistingMedia"]) && FileExists(irn, fileFormat))
            {
                stopwatch.Stop();
                Log.Logger.Debug("Found existing image {Irn} in {ElapsedMilliseconds} ms", irn, stopwatch.ElapsedMilliseconds);

                thumbnailType = null;
                return true;
            }

            try
            {
                using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");

                    var destPath = PathHelper.MakeDestPath(irn, fileFormat);

                    using (var fileStream = resource["file"] as FileStream)
                    using (var file = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        fileStream.CopyTo(file);

                        // Reuse stream to determine thumbnail dimensions
                        fileStream.Position = 0;
                        var imageInfo = new MagickImageInfo(fileStream);

                        if (imageInfo.Height == 324 && imageInfo.Width == 516)
                            thumbnailType = StandingStrongThumbnailType.full;
                        else if (imageInfo.Height == 494 && imageInfo.Width == 250)
                            thumbnailType = StandingStrongThumbnailType.triple;
                        else if(imageInfo.Height == 324 && imageInfo.Width == 250)
                            thumbnailType = StandingStrongThumbnailType.@double;
                        else if(imageInfo.Height == 154 && imageInfo.Width == 250)
                            thumbnailType = StandingStrongThumbnailType.single;
                        else
                            throw new Exception("Unexpected resolution found for Standing Strong thumbnail");
                    }
                }

                stopwatch.Stop();
                Log.Logger.Debug("Completed {fileFormat} irn {irn} creation in {ElapsedMilliseconds}ms", fileFormat, irn, stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error saving {fileFormat} irn {irn}", fileFormat, irn);
            }

            thumbnailType = null;
            return false;
        }

        private static bool FileExists(long irn, IEnumerable<MediaJob> mediaJobs)
        {
            // check whether media exists on disk for all media jobs
            return mediaJobs.All(mediaJob => File.Exists(PathHelper.MakeDestPath(irn, mediaJob.FileFormat, mediaJob.Derivative)));
        }

        private static bool FileExists(long irn, FileFormatType fileFormat)
        {
            // check whether media exists on disk for all media jobs
            return File.Exists(PathHelper.MakeDestPath(irn, fileFormat));
        }
    }
}
