using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace StarwingMapVisualizer.Misc
{
	internal static class AvaloniaBridge
	{
		public static Window MainWindow(this Application avaloniaApp)
		{
			var desktop = avaloniaApp.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			return desktop?.MainWindow;
		}
	}
}
