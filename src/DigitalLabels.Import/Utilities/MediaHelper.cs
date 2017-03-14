using System;
using System.Diagnostics;
using System.IO;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Factories;
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
                        // if image transform has been supplied it must be an image, otherwise it is another media type, so simply write file to disk
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
                Log.Logger.Debug("Completed {fileFormat} irn {irn} creation in {ElapsedMilliseconds}ms", fileFormat, irn, stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error saving {fileFormat} irn {irn}", fileFormat, irn);
            }

            return false;
        }

        //public static StandingStrongThumbnailType GetStandingStrongThumbnailType(FileStream fileStream)
        //{
        //    var imageInfo = new MagickImageInfo(fileStream);            

        //    if (imageInfo.Height == 230 && imageInfo.Width == 368)
        //        return StandingStrongThumbnailType.full;
        //    if (imageInfo.Height == 350 && imageInfo.Width == 179)
        //        return StandingStrongThumbnailType.triple;
        //    if (imageInfo.Height == 230 && imageInfo.Width == 179)
        //        return StandingStrongThumbnailType.@double;
        //    if (imageInfo.Height == 110 && imageInfo.Width == 179)
        //        return StandingStrongThumbnailType.single;

        //    throw new Exception("Unexpected resolution found for Standing Strong thumbnail");
        //}
    }
}
