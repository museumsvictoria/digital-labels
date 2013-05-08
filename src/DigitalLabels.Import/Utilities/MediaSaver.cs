using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Import.Factories;
using IMu;
using ImageResizer;
using NLog;

namespace DigitalLabels.Import.Utilities
{
    public static class MediaSaver
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static bool Save(FileStream fileStream, long irn, FileFormatType fileFormat, ResizeSettings resizeSettings, string derivative = null, bool reuseStream = false)
        {
            if (fileStream != null)
            {
                try
                {
                    var destPath = PathFactory.GetDestPath(irn, fileFormat, derivative);
                    var destPathDir = destPath.Remove(destPath.LastIndexOf('\\') + 1);

                    // Create directory
                    if (!Directory.Exists(destPathDir))
                    {
                        Directory.CreateDirectory(destPathDir);
                    }

                    // Delete file if it exists as we want to ensure it is overwritten
                    if (File.Exists(destPath))
                    {
                        File.Delete(destPath);
                    }

                    // Save file
                    if (resizeSettings != null)
                    {
                        ImageBuilder.Current.Build(fileStream, destPath, resizeSettings, !reuseStream);

                        if (reuseStream)
                            fileStream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                        fileStream.CopyTo(File.Create(destPath));

                    return true;
                }
                catch (Exception exception)
                {
                    // log error
                    _log.Error("Error saving image {0}, un-recoverable error, {1}", irn, exception.ToString());
                }
            }

            return false;
        }
    }
}
