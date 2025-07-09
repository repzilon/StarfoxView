using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StarFoxMapVisualizer.Misc
{
	public static class GraphicHelpers
	{
		public static Int32Rect FindImageRectangle(this BitmapImage image, byte alphaThreshold)
		{
			bool blnIsEdge = true;
			int x = 0, y;
			int width = image.PixelWidth;
			int height = image.PixelHeight;
			BitArray baIsEdge = new BitArray(checked(width * height));
			var pixelMatrix = CopyPixels(image);

			for (int i = 0; i < height; i++) {
				for (int j = 0; j < width; j++) {
					// Because of how the pixels are copied, we have to transpose our view
					baIsEdge[j * height + i] = (pixelMatrix[j, i].Alpha <= alphaThreshold);
					// If it was a GDI+ Bitmap, the right hand side would be gdipBitmap.GetPixel(i, j)
				}
			}

#if false
			var bytarEdge = new byte[checked(width * height / 8)];
			baIsEdge.CopyTo(bytarEdge, 0);
			System.IO.File.WriteAllBytes("edge.raw", bytarEdge);
#endif

			Padding padMargins = new Padding();

			while (blnIsEdge && (x < height)) {
				y = 0;
				while (blnIsEdge && (y < width)) {
					blnIsEdge &= baIsEdge[x * width + y];
					y++;
				}
				if (blnIsEdge) {
					padMargins.Left++;
				}
				x++;
			}

			blnIsEdge = true;
			x = height - 1;
			while (blnIsEdge && (x >= 0)) {
				y = 0;
				while (blnIsEdge && (y < width)) {
					blnIsEdge &= baIsEdge[x * width + y];
					y++;
				}
				if (blnIsEdge) {
					padMargins.Right++;
				}
				x--;
			}

			blnIsEdge = true;
			y = 0;
			while (blnIsEdge && (y < width)) {
				x = 0;
				while (blnIsEdge && (x < height)) {
					blnIsEdge &= baIsEdge[x * width + y];
					x++;
				}
				if (blnIsEdge) {
					padMargins.Top++;
				}
				y++;
			}

			blnIsEdge = true;
			y = width - 1;
			while (blnIsEdge && (y >= 0)) {
				x = 0;
				while (blnIsEdge && (x < height)) {
					blnIsEdge &= baIsEdge[x * width + y];
					x++;
				}
				if (blnIsEdge) {
					padMargins.Bottom++;
				}
				y--;
			}

			// And the transposed view has an effect here too.
			// It would be padMargins.Left, padMargins.Top,
			// width - padMargins.Horizontal, height - padMargins.Vertical otherwise
			return new Int32Rect(padMargins.Top, padMargins.Left,
				width - padMargins.Vertical,
				height - padMargins.Horizontal);
		}

		private static PixelColor[,] CopyPixels(this BitmapSource source)
		{
			var format = source.Format;
			var bgra32 = PixelFormats.Bgra32;
			if (format != bgra32) {
				source = new FormatConvertedBitmap(source, bgra32, null, 0);
			}

			var width = source.PixelWidth;
			var height = source.PixelHeight;
			PixelColor[,] pixels = new PixelColor[width, height];
			GCHandle pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			source.CopyPixels(
				new Int32Rect(0, 0, width, height),
				pinnedPixels.AddrOfPinnedObject(),
				width * height * 4,
				width * ((format.BitsPerPixel + 7) / 8));
			pinnedPixels.Free();
			return pixels;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct PixelColor
	{
		// 32 bit BGRA 
		[FieldOffset(0)] public UInt32 ColorBGRA;
		// 8 bit components
		[FieldOffset(0)] public byte Blue;
		[FieldOffset(1)] public byte Green;
		[FieldOffset(2)] public byte Red;
		[FieldOffset(3)] public byte Alpha;

		public override string ToString()
		{
			return String.Format("color: #{0:x2}{1:x2}{2:x2}; opacity: {3:g3}",
				this.Red, this.Green, this.Blue, this.Alpha * (1.0 / 255.0));
		}
	}

	internal struct Padding
	{
		public int Top;
		public int Bottom;
		public int Left;
		public int Right;

		public int Horizontal => Left + Right;

		public int Vertical => Top + Bottom;
	}
}
