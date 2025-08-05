namespace HL.Resources
{
	using System.IO;
	using HL.Manager;

	internal class HLResources
	{
		/// <summary>
		/// Open a <see cref="Stream"/> object to an internal resource (eg: xshd file)
		/// to load its contents from an 'Embedded Resource'.
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Stream OpenStream(string prefix, string name)
		{
			string fileRef = prefix + "." + name;

			Stream s = typeof(HLResources).Assembly.GetManifestResourceStream(fileRef);
			if (s == null)
				throw new FileNotFoundException("The resource file '" + fileRef + "' was not found.");

			return s;
		}

		/// <summary>
		/// Registers the built-in highlighting definitions on first time request for a definition
		/// or when the application changes its WPF Theme (e.g. from 'Light' to 'Dark') to load the
		/// appropriate highlighting resource when queried for it.
		/// </summary>
		/// <param name="hlm"></param>
		/// <param name="theme"></param>
		internal static void RegisterBuiltInHighlightings(
			DefaultHighlightingManager hlm,
			IHLTheme theme)
		{
			// This registration was already performed for this highlighting theme
			if (theme.IsBuiltInThemesRegistered == true)
				return;

			// XmlDoc is required by C#
			hlm.RegisterHighlighting(theme, "XmlDoc", null, "XmlDoc.xshd");
			// C# is required for MarkDown
			hlm.RegisterHighlighting(theme, "C#", new[] { ".cs" }, "CSharp-Mode.xshd");

			hlm.RegisterHighlighting(theme, "C/C++", new[] { ".c", ".h", ".cc", ".cpp", ".hpp" }, "CPP-Mode.xshd");
			hlm.RegisterHighlighting(theme, "MarkDown", new[] { ".md" }, "MarkDown-Mode.xshd");
			hlm.RegisterHighlighting(theme, "DOS/Windows batch", new[] { ".bat", ".cmd" }, "DOSBATCH.xshd");

			hlm.RegisterHighlighting(theme, "SuperFX and 65c816 Assembly (Argonaut syntax)", new[] { ".asm", ".inc", ".ext", ".mc" }, "GSUandSCPUAssembly.xshd");

			theme.IsBuiltInThemesRegistered = true;
		}
	}
}
