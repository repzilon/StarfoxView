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
	}
}
