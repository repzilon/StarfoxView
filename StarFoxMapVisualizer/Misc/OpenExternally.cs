using System;
using System.Diagnostics;
using System.IO;

namespace StarFoxMapVisualizer.Misc
{
	internal static class OpenExternal
	{
		public static Process Folder(string path, bool exploring = false)
		{
			if (!String.IsNullOrWhiteSpace(path)) {
				var strOption = exploring ? "/n,/e,/root," : "/n,";
				if (Directory.Exists(path)) {
					return Process.Start("explorer.exe", strOption + path);
				} else if (File.Exists(path)) {
					return Process.Start("explorer.exe", strOption + Path.GetDirectoryName(path));
				}
			}

			return null;
		}

		public static Process AssociatedProgram(string path)
		{
			if (!String.IsNullOrWhiteSpace(path)) {
				if (Directory.Exists(path)) {
					return Process.Start("explorer.exe", "/n," + path);
				}
				else if (File.Exists(path)) {
					var psi = new ProcessStartInfo(path);
					psi.UseShellExecute = true;
					return Process.Start(psi);
				}
			}

			return null;
		}

		public static Process TextEditor(string path)
		{
			if (!String.IsNullOrWhiteSpace(path) && File.Exists(path)) {
				return Process.Start("notepad.exe", path);
			}

			return null;
		}
	}
}
