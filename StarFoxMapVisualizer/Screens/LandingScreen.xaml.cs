using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for LandingScreen.xaml
    /// </summary>
    public partial class LandingScreen : Page
    {
        const string RecentTXTFileName = "recent.txt";
        bool RecentExists => File.Exists(RecentTXTFileName);

        public LandingScreen()
        {
            InitializeComponent();

            if (!RecentExists)
                ClearRecentFile.Visibility = Visibility.Collapsed;
        }

        private async void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            GetStartedButton.IsEnabled = false;
            string fileLoc = default;
            if (RecentExists)
                fileLoc = File.ReadAllText(RecentTXTFileName);

            bool result = false;
            for (int retries = 0; retries < 1; retries++)
            {
                if (fileLoc == default)
                { // SHOW FILE BROWSER
                    var dialog = new OpenFileDialog()
                    {
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Title = "Select any file in StarFox source base directory"
                    };
                    if (dialog.ShowDialog() == true) {
	                    if (File.Exists(dialog.FileName)) {
		                    fileLoc = Path.GetDirectoryName(dialog.FileName);
	                    } else if (Directory.Exists(dialog.FileName)) {
		                    fileLoc = dialog.FileName;
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
                if (await AppResources.TryImportProject(new DirectoryInfo(fileLoc)))
                {
                    result = true;
                    break; // loading the project success, break out
                }
                fileLoc = default;
            }

            if (!result) return;

            //SET NEW RECENT FILE
#if NETFRAMEWORK
            File.WriteAllText(RecentTXTFileName, fileLoc);
#else
            await File.WriteAllTextAsync(RecentTXTFileName, fileLoc);
#endif

            EditScreen screen = new EditScreen();
            //InstrumentPackerControl screen = new();

            //Set the Editor Screen Current Instance
            EDITORStandard.CurrentEditorScreen = screen;
            ((MainWindow)Application.Current.MainWindow).Content = screen;
        }

        private void ClearRecentFile_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(RecentTXTFileName);
            ClearRecentFile.Visibility = Visibility.Collapsed;
        }
    }
}
