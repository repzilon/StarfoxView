using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
			this.Loaded += AboutBox_Loaded;
        }

		private void AboutBox_Loaded(object sender, RoutedEventArgs e)
		{
			using (var app = Process.GetCurrentProcess()) {
				OutputMemoryUsage(app.PrivateMemorySize64);
			}
		}

		private void GithubLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://github.com/JDrocks450");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

		private void GC_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			long lngBefore, lngAfter;
			using (var app = Process.GetCurrentProcess()) {
				lngBefore = app.PrivateMemorySize64;
			}
			OutputMemoryUsage(lngBefore);
			GC.Collect();
			using (var app = Process.GetCurrentProcess()) {
				lngAfter = app.PrivateMemorySize64;
			}
			OutputMemoryUsage(lngAfter, lngAfter - lngBefore);
		}

		private void OutputMemoryUsage(long commitBytes)
		{
			this.lblMemory.Text = $"Commit size: {commitBytes * (1.0 / (1024.0 * 1024.0)):g4} MiB";
		}

		private void OutputMemoryUsage(long commitBytes, long delta)
		{
			const double kOneMillionth = 1.0 / (1024.0 * 1024.0);
			this.lblMemory.Text = $"Commit size: {commitBytes * kOneMillionth:f1} MiB ({delta * kOneMillionth:f2} MiB)";
		}
	}
}
