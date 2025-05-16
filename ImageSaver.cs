using System;
using System.Drawing;
using System.IO;

namespace QRCodeGenerator
{
    public static class ImageSaver
    {
        /// <summary>
        /// Saves a Bitmap image to the specified path. The format is determined by the file extension (.png or .jpg).
        /// </summary>
        /// <param name="image">The Bitmap image to save.</param>
        /// <param name="path">The file path where the image will be saved.</param>
        public static void SaveImage(Bitmap image, string path)
        {
            if (image == null)
            {
                throw new ArgumentException("Image cannot be null");
            }

            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var format = ext == ".jpg" || ext == ".jpeg"
                ? System.Drawing.Imaging.ImageFormat.Jpeg
                : System.Drawing.Imaging.ImageFormat.Png;

            image.Save(path, format);
        }
    }
}