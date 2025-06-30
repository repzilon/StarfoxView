using System;
using System.Text;
using StarFox.Interop.MISC;

namespace StarFox.Interop.MSG
{
	/// <summary>
	/// Translates character coding between standard Unicode and StarFox MOJI_0.* files for Japanese text
	/// </summary>
	/// <remarks>
	/// Source file is encoded in UTF-8 and contains Japanese characters (mainly katakana).
	/// If you see mojibake (garbled text) when viewing this file:
	/// you try to take the best of me, GO AWAY then もちあげて､ ときはまして｡
	/// I will neither bake a cake for you, MARIO, nor cook CHIPs.
	/// </remarks>
	public static class MojiZeroTranslator
	{
		/// <summary>
		/// Characters shown in MOJI_0.bmp found in UltraStarFox source
		/// </summary>
		/// <remarks>
		/// This constant is laid out in 7-character rows in source code, just like the bitmap image.
		/// String index also matches the tile number, so the first character (at index 0) maps to
		/// tile number 0 in the font, and so on. Tile numbers follow European text reading order.
		/// </remarks>
		private const string Moji0Tiles =
"0123456" +
"789ABCD" +
"EFGHIJK" +
"LMNOPQR" +
"STUVWXY" +
"Z!%. ab" +
"cdefghi" +
"jklmnop" +
"qrstuvw" +
"xyzー.'?" +
"‼Rアイウエオ" +
"カキクケコサシ" +
"スセソタチツテ" +
"トナニヌネノハ" +
"ヒフヘホマミム" +
"メモヤユヨラリ" +
"ルレロワヲンﾞ" +
"ﾟ。〜ァィゥェ" +
"ォャュョッ,;" +
"!?,・ABL";

		// Dakuten and handakuten also apply to hiragana, but StarFox only uses katakana.
		private const string DakutenableKana = "カキクケコサシスセソタチツテトハヒフヘホ";
		private const string DakutenedKana = "ガギグゲゴザジズゼゾダヂヅデドバビブベボ";
		private const string HandakutenableKana = "ハヒフヘホ";
		private const string HandakutenedKana = "パピプペポ";

		// TODO : this method does not do the job it is supposed to
		public static string MergeDakutens(string text)
		{
			int i;
			string unaccented, accented;
			for (i = 0; i < DakutenableKana.Length; i++) {
				unaccented = DakutenableKana.CharAt(i);
				accented = DakutenedKana.CharAt(i);
				text = text.Replace(unaccented + "\u3099", accented).Replace(unaccented + "\u309B", accented).Replace(unaccented + "\uff9e", accented);

			}
			for (i = 0; i < HandakutenableKana.Length; i++) {
				unaccented = HandakutenableKana.CharAt(i);
				accented = HandakutenedKana.CharAt(i);
				text = text.Replace(unaccented + "\u309A", accented).Replace(unaccented + "\u309C", accented).Replace(unaccented + "\uff9f", accented);
			}

			return text;
		}

		public static string SplitDakutens(string text)
		{
			int i;
			for (i = 0; i < DakutenedKana.Length; i++) {
				text = text.Replace(DakutenedKana.CharAt(i), DakutenableKana.CharAt(i) + "\u3099");
			}
			for (i = 0; i < HandakutenedKana.Length; i++) {
				text = text.Replace(HandakutenedKana.CharAt(i), HandakutenableKana.CharAt(i) + "\u309A");
			}

			return text;
		}

		public static string Decode(string mojiText, TRNFile translator)
		{
			StringBuilder stbJapanese = new StringBuilder(mojiText.Length);
#if NETFRAMEWORK || NETSTANDARD
			var charSet = Encoding.Default;
#else
			var charSet = Encoding.Latin1;
#endif
			var bytarOriginal = charSet.GetBytes(mojiText);
			for (int i = 0; i < bytarOriginal.Length; i++) {
				var ch = bytarOriginal[i];
				if (ch > 32) {
					var t = translator.TileNumberFor(ch);
					stbJapanese.Append(Moji0Tiles[t]);
				} else {
					stbJapanese.Append((char)ch);
				}
			}
#if DEBUG
			var strJap = stbJapanese.ToString();
			strJap = MergeDakutens(strJap);
			return strJap;
#else
			return MergeDakutens(stbJapanese.ToString());
#endif
		}

		public static bool IsMojibake(string text)
		{
			if (String.IsNullOrEmpty(text)) {
				return false;
			} else {
				var c = text.Length;
				var n = 0;
				for (int i = 0; i < c; i++) {
					if (Char.IsLetter(text[i])) {
						n++;
					}
				}

				// ReSharper disable once IntDivisionByZero (there is a length check above)
				return (100 * n / c) < 50; // mojibake if less than 50% of letters
			}
		}
	}
}
