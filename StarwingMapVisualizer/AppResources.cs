using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;
using StarwingMapVisualizer.Dialogs;

namespace StarwingMapVisualizer
{
	/// <summary>
	/// Resources that can be accessed throughout all the User Interface code
	/// </summary>
	internal static class AppResources
	{
		public const string ApplicationName = "SF-View";

		public static string GetTitleLabel
		{
			get
			{
				try {
					string version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location ?? "")
						?.FileVersion;
					return $"{ApplicationName} on Avalonia | v{version ?? "Error"} [αλφα]";
				} catch (Exception ex) {
					MessageBox.Show("Polling for a version number did not complete successfully.\n" + ex.Message);
				}

				return ApplicationName;
			}
		}

		/// <summary>
		/// Dictates whether the <see cref="MAPImporter"/> can automatically find referenced level sections and populate them
		/// </summary>
		public static bool MapImporterAutoDereferenceMode = true;

		/// <summary>
		/// Files that are marked as *include files, as in containing symbol information
		/// </summary>
		public static HashSet<ASMFile> Includes => ImportedProject?.Includes;

		/// <summary>
		/// All files that have been imported by the <see cref="ASMImporter"/>
		/// <para/>Key is File FullName (FullPath)
		/// </summary>
		public static Dictionary<string, IImporterObject> OpenFiles => ImportedProject?.OpenFiles;

		public static IEnumerable<MAPFile> OpenMAPFiles => ImportedProject?.OpenMAPFiles;

		public static bool IsFileIncluded(FileInfo File) => ImportedProject?.IsFileIncluded(File) ?? false;

		/// <summary>
		/// The project imported by the user, if one has been imported already
		/// <para>See: </para>
		/// </summary>
		public static SFCodeProject ImportedProject { get; internal set; }

		/// <summary>
		/// Attempts to load a project from the given source code folder
		/// </summary>
		/// <param name="ProjectDirectory"></param>
		/// <returns></returns>
		public static async Task<bool> TryImportProject(DirectoryInfo ProjectDirectory)
		{
			try {
				var codeProject = new SFCodeProject(ProjectDirectory.FullName);
				await codeProject.EnumerateAsync(); // populate the project with files and folders
				ImportedProject = codeProject;
			} catch (Exception ex) {
#if DEBUG
				MessageBox.Show(ex.Message);
#endif
				return false;
			}

			return true;
		}

		/// <summary>
		/// Shows the CrashWindow with the given parameters
		/// </summary>
		/// <param name="Exception"></param>
		/// <param name="Fatal"></param>
		/// <param name="Tip"></param>
		internal static bool? ShowCrash(Exception Exception, bool Fatal, string Tip)
		{
			// TODO : Implement AppResources.ShowCrash
			throw new NotImplementedException();
			/*
			CrashWindow window = new CrashWindow(Exception, Fatal, Tip) {
				WindowStartupLocation = WindowStartupLocation.CenterScreen
			};
			return window.ShowDialog();
			// */
		}
	}
}
