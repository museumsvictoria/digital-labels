using System;
using System.Diagnostics;
using System.IO;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Infrastructure;
using ImageMagick;
using IMu;
using Serilog;

namespace DigitalLabels.Import.Utilities
{
    public static class MediaHelper
    {
        public static bool TrySaveMedia(long irn, FileFormatType fileFormat, Func<MagickImage, MagickImage> imageTransform = null, string derivative = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");

                    using (var fileStream = resource["file"] as FileStream)
                    using (var file = File.Open(PathHelper.MakeDestPath(irn, fileFormat, derivative), FileMode.Create, FileAccess.ReadWrite))
                    {
                        // if image transform has been supplied apply transform if not simply write file to disk
                        if (imageTransform != null)
                        {
                            using (var image = imageTransform(new MagickImage(fileStream)))
                            {
                                image.Write(file);
                            }
                        }
                        else
                        {
                            fileStream.CopyTo(file);
                        }
                    }
                }

                stopwatch.Stop();
                Log.Logger.Debug("Completed {fileFormat} {derivative} irn {irn} creation in {ElapsedMilliseconds}ms", fileFormat, derivative ?? string.Empty, irn, stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error saving {fileFormat} irn {irn}", fileFormat, irn);
            }

            return false;
        }

        public static bool TrySaveStandingStrongThumbnail(long irn, FileFormatType fileFormat, out StandingStrongThumbnailType? thumbnailType)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

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

                        if (imageInfo.Height == 230 && imageInfo.Width == 368)
                            thumbnailType = StandingStrongThumbnailType.full;
                        else if (imageInfo.Height == 350 && imageInfo.Width == 179)
                            thumbnailType = StandingStrongThumbnailType.triple;
                        else if(imageInfo.Height == 230 && imageInfo.Width == 179)
                            thumbnailType = StandingStrongThumbnailType.@double;
                        else if(imageInfo.Height == 110 && imageInfo.Width == 179)
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
    }
}
