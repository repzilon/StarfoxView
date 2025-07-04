//render activated? requires adding support for System.Drawing.Common which really isn't necessary at this moment
//#define RENDER

using System;
#if RENDER
using System.Drawing;
#endif
using System.Linq;

// ************************************
// THANK YOU LUIGIBLOOD!
// Using open source hcgcad
// https://github.com/LuigiBlood/hcgcad
// ************************************

namespace StarFox.Interop.MISC
{
	public static class Utility
	{
		//Handy stuff
		public static T[] Subarray<T>(T[] obj, int i, int len)
		{
			T[] output = new T[len];

			for (int j = 0; j < len; j++) {
				output[j] = obj[(i + j) % obj.Length];
			}

			return output;
		}

		public static bool[] ToBitStream(byte[] t, int bits)
		{
			bool[] bitstr = new bool[bits];
			for (int i = 0; i < bits; i++)
				bitstr[i] = (((t[i / 8] >> (i % 8)) & 1) != 0);
			return bitstr;
		}

		public static bool[] ToBitStreamReverse(byte[] t, int bits)
		{
			bool[] bitstr = new bool[bits];
			for (int i = 0; i < bits; i++)
				bitstr[i] = (((t[i / 8] << (i % 8)) & 0x80) != 0);
			return bitstr;
		}

		public static byte[] ToByteStream(bool[] bits)
		{
			byte[] bytes = new byte[Convert.ToInt32(Math.Ceiling((decimal)(bits.Length / 8.0)))];
			for (int i = 0; i < bits.Length; i++) {
				byte test = (byte)((bits[i]) ? (1 << (i % 8)) : 0);
				bytes[i / 8] |= test;
			}
			return bytes;
		}
#if RENDER
		//Get Rectangle for Cropping
		public static Rectangle GetBoundingRect(Bitmap[] imgs)
		{
			if (imgs.Length == 0)
				return new Rectangle(0, 0, 0, 0);

			Rectangle rect = new Rectangle(imgs[0].Width, imgs[0].Height, 0, 0);

			//Find Base
			foreach (Bitmap b in imgs) {
				for (int y = 0; y < b.Height; y++) {
					for (int x = 0; x < b.Width; x++) {
						Color pixel = b.GetPixel(x, y);
						if (pixel.A != 0) {
							rect.X = (x < rect.X) ? x : rect.X;
							rect.Y = (y < rect.Y) ? y : rect.Y;
						}
					}
				}
			}

			//Width & Height
			foreach (Bitmap b in imgs) {
				for (int y = b.Height - 1; y >= 0; y--) {
					for (int x = b.Width - 1; x >= 0; x--) {
						Color pixel = b.GetPixel(x, y);
						if (pixel.A != 0) {
							rect.Width = ((x - rect.X + 1) > rect.Width) ? (x - rect.X + 1) : rect.Width;
							rect.Height = ((y - rect.Y + 1) > rect.Height) ? (y - rect.Y + 1) : rect.Height;
						}
					}
				}
			}

			return rect;
		}
#endif

		#region Repzilon added utility methods
		public static bool AnyOf<T>(T toCheck, T first, T second) where T : IEquatable<T>
		{
			return toCheck.Equals(first) || toCheck.Equals(second);
		}

		public static bool AnyOf<T>(T toCheck, T first, T second, T third) where T : IEquatable<T>
		{
			return toCheck.Equals(first) || toCheck.Equals(second) || toCheck.Equals(third);
		}

		public static bool AnyOf<T>(T toCheck, params T[] allowed) where T : IEquatable<T>
		{
			return allowed.Contains(toCheck);
		}

		public static T[] GetValues<T>() where T : struct
		{
			return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
		}

		private static bool NotNulByte(byte value)
		{
			return value != 0;
		}

		public static byte[] TrimEnd(/*this*/ byte[] array)
		{
			var lastNonNul = Array.FindLastIndex(array, NotNulByte);
			if (lastNonNul >= 0) {
				Array.Resize(ref array, lastNonNul + 1);
			}
			return array;
		}
		#endregion
	}
}
