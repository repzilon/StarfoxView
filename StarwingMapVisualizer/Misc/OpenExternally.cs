using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace StarwingMapVisualizer.Misc
{
	internal static class OpenExternal
	{
		public static Process Folder(string path, bool exploring = false)
		{
			if (!String.IsNullOrWhiteSpace(path)) {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					var strOption = exploring ? "/n,/e,/root," : "/n,";
					if (Directory.Exists(path)) {
						return Process.Start("explorer.exe", strOption + path);
					} else if (File.Exists(path)) {
						return Process.Start("explorer.exe", strOption + Path.GetDirectoryName(path));
					}
				} else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
					if (Directory.Exists(path)) {
						return Process.Start("open", path);
					} else if (File.Exists(path)) {
						return Process.Start("open", Path.GetDirectoryName(path));
					}
				}
			}

			return null;
		}

		public static Process AssociatedProgram(string path)
		{
			if (!String.IsNullOrWhiteSpace(path)) {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					if (Directory.Exists(path)) {
						return Process.Start("explorer.exe", "/n," + path);
					} else if (File.Exists(path)) {
						return Process.Start(WindowsShellExecute(path));
					}
				} else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
					return Process.Start("open", path);
				}
			}

			return null;
		}

		public static Process TextEditor(string path)
		{
			if (!String.IsNullOrWhiteSpace(path) && File.Exists(path)) {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					return Process.Start("notepad.exe", path);
				} else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
					return Process.Start("open", "-e " + path);
				}
			}

			return null;
		}

		public static Process WebPage(string url)
		{
			if (!String.IsNullOrWhiteSpace(url)) {
				// https://github.com/AvaloniaCommunity/MessageBox.Avalonia
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					return Process.Start(WindowsShellExecute(url));
				} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
					return Process.Start("x-www-browser", url);
				} else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
					return Process.Start("open", url);
				}
			}

			return null;
		}

		private static ProcessStartInfo WindowsShellExecute(string path)
		{
			var psi = new ProcessStartInfo(path);
			psi.UseShellExecute = true;
			return psi;
		}
	}
}
