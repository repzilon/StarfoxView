namespace StarFox.Interop.MISC
{
	public static class StringExtensions
	{
		public static string RemoveEscapes(this string input)
		{
			return input.Replace("\t", "  ");
		}

		public static string NormalizeFormatting(this string input, bool removeEscapes)
		{
			if (removeEscapes) {
				input = input.RemoveEscapes();
			}
			input = input.Trim();
			// recursive remove unnecessary spaces
			while (input.Contains("  ")) {
				input = input.Replace("  ", " ");
			}
			return input;
		}

		public static string NormalizeFormatting(this string input)
		{
			return NormalizeFormatting(input, true);
		}

		/// <summary>
		/// When you need one character but returned as a string, unlike the indexer.
		/// </summary>
		/// <param name="text">The full string</param>
		/// <param name="i">Zero-based index of the character in text.</param>
		/// <returns>A one-character string</returns>
		internal static string CharAt(this string text, int i)
		{
			return text.Substring(i, 1);
		}
	}
}
