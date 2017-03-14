using System;
using System.Configuration;
using System.IO;
using DigitalLabels.Core.DomainModels;
using Serilog;

namespace DigitalLabels.Import.Utilities
{
    public static class PathHelper
    {
        public static string MakeDestPath(long irn, FileFormatType fileFormat, string derivative = null)
        {
            var directory = $"{ConfigurationManager.AppSettings["MediaPath"]}\\{GetSubFolder(irn)}\\";

            CreateDirectory(directory);

            return $"{directory}\\{GetFileName(irn, fileFormat, derivative)}";
        }

        public static string GetUrlPath(long irn, FileFormatType fileFormat, string derivative = null)
        {
            return $"{ConfigurationManager.AppSettings["MediaServerUrl"]}media/{GetSubFolder(irn)}/{GetFileName(irn, fileFormat, derivative)}";
        }

        private static int GetSubFolder(long id)
        {
            return (int)(id % 10);
        }

        private static string GetFileName(long irn, FileFormatType fileFormat, string derivative)
        {
            return derivative == null ? $"{irn}.{fileFormat.ToString().ToLower()}" : $"{irn}-{derivative}.{fileFormat.ToString().ToLower()}";
        }

        private static void CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var directory = Path.GetDirectoryName(path);

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Error creating {directory} directory", directory);
                throw;
            }
        }
    }
}