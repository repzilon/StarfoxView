using System.Windows;
using System.Windows.Threading;
using StarFoxMapVisualizer.Dialogs;

namespace StarFoxMapVisualizer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		//ERROR HANDLER ONLY AVAILABLE IN RELEASE BUILD
#if !DEBUG
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
