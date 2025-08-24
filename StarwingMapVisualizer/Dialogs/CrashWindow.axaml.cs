using System;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace StarwingMapVisualizer.Dialogs
{
	public partial class CrashWindow : Window
	{
		private static readonly string[] Messages = {
			"sorry about this",
			"again? really?",
			"this must be aggravating",
			"i apologize",
			"try not doing that",
			"found a problem",
			"that's an issue",
			"this probably shouldn't happen",
			"an error occured.",
			"sample text"
		};

		private static readonly Random _rng = new Random();

		public CrashWindow(Exception exception, bool Fatal = true, string Tip = "Looks like StarfoxView crashed.")
		{
			InitializeComponent();

			//setup dialog
			NonfatalButton.IsVisible = false;
			MessageBlock.Inlines?.Insert(0, new Run(Tip));

			if (!Fatal) {
				Background               = Brushes.Gray;
				NonfatalButton.IsVisible = true;
				CloseButton.IsVisible    = false;
				ContinueButton.IsVisible = false;
			}

			RandomTitle();
			ErrorBox.Text = exception.ToString();
		}

		void RandomTitle()
		{
			var titleIndex = _rng.Next(0, Messages.Length - 1);
			Title = Messages[titleIndex].ToUpper();
		}

		private void ContinueButton_Click(object sender, RoutedEventArgs e)
		{
			Close(false);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close(true);
		}

		private void Window_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
		{
			//easter egg i guess
			RandomTitle();
		}
	}
}
