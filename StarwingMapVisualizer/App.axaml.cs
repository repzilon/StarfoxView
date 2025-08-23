using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void AppAbout_OnClick(object sender, EventArgs e)
		{
			new AboutBox().ShowDialog(Current.MainWindow());
		}

		private void AppPreferences_OnClick(object sender, EventArgs e)
		{
			var settings = new SettingsDialog();
			settings.Show();
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				desktop.MainWindow = new MainWindow();
			}

			base.OnFrameworkInitializationCompleted();
		}

		//TODO : ERROR HANDLER ONLY AVAILABLE IN RELEASE BUILD
#if false
        public App()
        {
            DispatcherUnhandledException += RootError;
        }

        private void RootError(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            CrashWindow window = new CrashWindow(e.Exception)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            if (window.ShowDialog() ?? true)
            { // CLOSE
                Application.Current.Shutdown();
                return;
            }
            //IGNORE
            e.Handled = true;
        }
#endif
	}
}
