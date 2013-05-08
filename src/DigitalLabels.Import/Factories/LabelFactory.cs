//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using DigitalLabels.Core.DomainModels;
//using IMu;
//using ImageResizer;
//using NLog;
//using DigitalLabels.Import.Utilities;

//namespace DigitalLabels.Import.Factories
//{
//    public class LabelFactory
//    {
//        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

//        public static bool TryFetchMedia(Session session, ManyNationsMedia media)
//        {
//            try
//            {
//                var multimedia = new Module("emultimedia", session);
//                multimedia.FindKey(media.Irn);
//                var result = multimedia.Fetch("start", 0, -1, new[] { "resource" }).Rows[0];
//                var resource = result.GetMap("resource");

//                var fileStream = resource["file"] as FileStream;
//                var identifier = resource.GetString("identifier");

//                var destPath = PathFactory.GetDestPath(media, identifier);

//                // Save file
//                SaveMedia(media, fileStream, destPath);

//                // Set filename after successful saving of image
//                media.Filename = PathFactory.GetUriPath(media, identifier);
//            }
//            catch (Exception exception)
//            {
//                // log error
//                _log.Error("Error saving image {0}, un-recoverable error, {1}", media.Irn, exception.ToString());
//                return false;
//            }

//            return true;
//        }

//        private static void SaveMedia(ManyNationsMedia media, FileStream fileStream, string path)
//        {
//            // Create directory
//            var pathDir = path.Remove(path.LastIndexOf('\\') + 1);
//            if (!Directory.Exists(pathDir))
//            {
//                Directory.CreateDirectory(pathDir);
//            }

//            // Delete file if it exists as we want to ensure it is overwritten
//            if (File.Exists(path))
//            {
//                File.Delete(path);
//            }

//            // Save file
//            switch (media.Type)
//            {
//                case "image":
//                    ImageBuilder.Current.Build(fileStream, path, new ResizeSettings { Format = "jpg", Quality = 85 });
//                    break;
//                default:
//                    fileStream.CopyTo(File.Create(path));
//                    break;
//            }
//        }
//    }
//}
