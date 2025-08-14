using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace StarFoxMapVisualizer.Misc
{
    internal static class WPFInteropExtensions
    {
        /// <summary>
        /// Takes a GDI+ bitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        [Obsolete]
        public static BitmapImage Convert(this Bitmap src, bool TransparentEnabled = true)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, TransparentEnabled ? ImageFormat.Png : ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        /// <summary>
        /// Takes a SKBitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        public static BitmapImage Convert(this SKBitmap src, bool TransparentEnabled = true)
        {
			MemoryStream ms = new MemoryStream();
			SKManagedWStream skms = new SKManagedWStream(ms, false);
			SKPixmap.Encode(skms, src, TransparentEnabled ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Bmp, 100);
	        BitmapImage image = new BitmapImage();
	        image.CacheOption = BitmapCacheOption.OnLoad;
	        image.BeginInit();
	        ms.Seek(0, SeekOrigin.Begin);
	        image.StreamSource = ms;
	        image.EndInit();
	        return image;
        }
	}
}
