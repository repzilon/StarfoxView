using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace StarwingMapVisualizer
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
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
