using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using StarwingMapVisualizer;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer.Screens
{
	public partial class LandingScreen : UserControl
	{
		private const string RecentTXTFileName = "recent.txt";

		private bool RecentExists => File.Exists(RecentTXTFileName);

		public LandingScreen()
		{
			InitializeComponent();

			if (!RecentExists) {
				ClearRecentFile.IsVisible = false;
			}
		}

		private async void GetStartedButton_Click(object sender, RoutedEventArgs e)
		{
			GetStartedButton.IsEnabled = false;
			string fileLoc = null;
			if (RecentExists) {
				fileLoc = File.ReadAllText(RecentTXTFileName);
			}

			bool result = false;
			for (int retries = 0; retries < 1; retries++) {
				if (fileLoc == null) { // SHOW FILE BROWSER
					var topLevel = TopLevel.GetTopLevel(this);
					var files    = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
					{
						Title         = "Select any file in StarFox source base directory",
						// TODO : Support initialDirectory in GetStartedButton_Click
						//SuggestedStartLocation = new IStorageFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
					});

					if (files.Any()) {
						var path = files.First().TryGetLocalPath();
						if (File.Exists(path)) {
							fileLoc = Path.GetDirectoryName(path);
						} else if (Directory.Exists(path)) {
							fileLoc = path;
						} else {
							GetStartedButton.IsEnabled = true;
							return;
						}
					} else {
						GetStartedButton.IsEnabled = true;
						return; // USER CANCELLED
					}
				}

				//TRY TO LOAD THE PROJECT
				if (await AppResources.TryImportProject(new DirectoryInfo(fileLoc))) {
					result = true;
					break; // loading the project success, break out
				}

				fileLoc = null;
			}

			if (!result) {
				return;
			}

			//SET NEW RECENT FILE
#if NETFRAMEWORK
            File.WriteAllText(RecentTXTFileName, fileLoc);
#else
			await File.WriteAllTextAsync(RecentTXTFileName, fileLoc);
#endif
			EditScreen screen = new EditScreen();
			EDITORStandard.CurrentEditorScreen                     = screen;
			((MainWindow)Application.Current.MainWindow()).Content = screen;
		}

		private void ClearRecentFile_Click(object sender, RoutedEventArgs e)
		{
			File.Delete(RecentTXTFileName);
			ClearRecentFile.IsVisible = false;
		}
	}
}
