using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer.Dialogs
{
	public partial class AboutBox : Window
	{
		private static readonly string kMemoryTypeLabel =
		 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "GC memory" : "Commit size";

		public AboutBox()
		{
			InitializeComponent();
			this.Loaded += AboutBox_Loaded;
		}

		private void AboutBox_Loaded(object sender, RoutedEventArgs e)
		{
			OutputMemoryUsage(CurrentMemoryUsage());
		}

		private void GithubLink_Click(object sender, RoutedEventArgs e)
		{
			OpenExternal.WebPage("https://github.com/JDrocks450");
		}

		private void Button_Click(object sender, PointerReleasedEventArgs e)
		{
			Close();
		}

		private void GC_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
		{
			long lngBefore = CurrentMemoryUsage();
			OutputMemoryUsage(lngBefore);
			GC.Collect();
			long lngAfter = CurrentMemoryUsage();
			OutputMemoryUsage(lngAfter, lngAfter - lngBefore);
		}

        private static long CurrentMemoryUsage()
        {
        	if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        		// Microsoft is too lazy to provide a libproc wrapper, but libproc is poorly documented.
        		return GC.GetTotalMemory(false);
        	} else {	// I don't know it is reliable on Linux
        		// A Process instance is more like a snapshot
        		using (var prcSelf = Process.GetCurrentProcess()) {
        			return prcSelf.PrivateMemorySize64;
        		}
        	}
        }

		private void OutputMemoryUsage(long commitBytes)
		{
			this.lblMemory.Text = $"{kMemoryTypeLabel}: {commitBytes * (1.0 / (1024.0 * 1024.0)):g4} MiB";
		}

		private void OutputMemoryUsage(long commitBytes, long delta)
		{
			const double kOneMillionth = 1.0 / (1024.0 * 1024.0);
			this.lblMemory.Text = $"{kMemoryTypeLabel}: {commitBytes * kOneMillionth:f1} MiB ({delta * kOneMillionth:f2} MiB)";
		}
	}
}
