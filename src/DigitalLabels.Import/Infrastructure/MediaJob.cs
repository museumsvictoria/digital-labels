using System;
using DigitalLabels.Core.DomainModels;
using ImageMagick;

namespace DigitalLabels.Import.Infrastructure
{
    public class MediaJob
    {
        public FileFormatType FileFormat { get; set; }

        public string Derivative { get; set; }

        public Func<MagickImage, MagickImage> ImageTransform { get; set; }
    }
}