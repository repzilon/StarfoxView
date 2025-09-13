using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace StarwingMapVisualizer.Misc
{
	internal static class AvaloniaBridge
	{
		public static Window MainWindow(this Application avaloniaApp)
		{
			var desktop = avaloniaApp.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			return desktop?.MainWindow;
		}

		private static TopLevel OurTopLevel()
		{
			return TopLevel.GetTopLevel(Application.Current.MainWindow());
		}

		public static IClipboard Clipboard()
		{
			return OurTopLevel().Clipboard;
		}
	}
}
