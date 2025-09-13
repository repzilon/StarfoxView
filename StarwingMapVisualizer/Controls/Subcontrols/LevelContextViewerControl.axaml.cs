using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using StarFox.Interop.EFFECTS;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MISC;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer.Controls.Subcontrols
{
	public partial class LevelContextViewerControl : UserControl
	{
		private double BG3X, BG3Y, BG2X, BG2Y, ScrWidth, ScrHeight;
		private bool IgnoreUserInput;

		public LevelContextViewerControl()
		{
			InitializeComponent();

			Loaded += delegate {
				//item selector for dynamic backgrounds
				DynamicBackgroundAnimationSelector.ItemsSource =
					Utility.GetValues<WavyBackgroundRenderer.WavyEffectStrategies>();
				PendingChangesMessage.IsVisible = false; // pending changes message for dynamic backgrounds
			};
			Unloaded += delegate { // Dispose of the Background Renderer as it is IDisposable
				ImageContent.Dispose();
			};
		}

		public MAPContextDefinition LevelContext { get; private set; }

		public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
		{
			if (levelContext == null) return;
			LevelContext = levelContext;

			BG3X                             = LevelContext.BG3.HorizontalOffset;
			BG3Y                             = LevelContext.BG3.VerticalOffset;
			BG2X                             = LevelContext.BG2.HorizontalOffset;
			BG2Y                             = LevelContext.BG2.VerticalOffset;
			IgnoreUserInput                  = true;
			ViewOptions.XScrollSlider.Value  = BG3X;
			ViewOptions.YScrollSlider.Value  = BG3Y;
			ViewOptions.XScrollSlider2.Value = BG2X;
			ViewOptions.YScrollSlider2.Value = BG2Y;
			IgnoreUserInput                  = false;

			ApplyButton.IsEnabled       = false;
			ContextDataGrid.ItemsSource = new[] { levelContext };
			await ImageContent.SetContext(LevelContext, WavyBackgroundRenderer.WavyEffectStrategies.None, ExtractCCR,
				ExtractPCR);
			ScrWidth = ScrHeight = ImageContent.Width = ImageContent.Height;

			DynamicBackgroundAnimationSelector.SelectionChanged -= DynamicBackgroundAnimationSelector_SelectionChanged;
			DynamicBackgroundAnimationSelector.SelectedIndex    =  0;
			DynamicBackgroundAnimationSelector.SelectionChanged += DynamicBackgroundAnimationSelector_SelectionChanged;

			PendingChangesMessage.IsVisible = false;
			LatencyBox.TextChanged -= LatencyBox_TextChanged;
			LatencyBox.Text = ImageContent.TargetFrameRate.TotalMilliseconds.ToString(CultureInfo.CurrentCulture);
			LatencyBox.TextChanged += LatencyBox_TextChanged;

			ResetViewSettings();
		}

		private void ResetViewSettings()
		{
			ImageContent.SetViewportsToUniformSize(ScrWidth, ScrHeight, BG2X, BG2Y, BG3X, BG3Y, 1024);
		}

		private void ContextDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			ApplyButton.IsEnabled = true;
		}

		private async void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			ApplyButton.IsEnabled = false;
			await Attach(LevelContext);
		}

		private void DynamicBackgroundAnimationSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DynamicBackgroundAnimationSelector.SelectedItem == null) return;
			WavyBackgroundRenderer.WavyEffectStrategies selection =
				(WavyBackgroundRenderer.WavyEffectStrategies)DynamicBackgroundAnimationSelector.SelectedItem;
			ImageContent.ChangeAnimationMode(selection);
		}

		private void LatencyBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = LatencyBox.Text;
			if (string.IsNullOrWhiteSpace(text)) return;
			if (!double.TryParse(text, out var milliseconds)) return;
			var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
			if (ImageContent.TargetFrameRate == timeSpan) return;
			ImageContent.TargetFrameRate    = timeSpan;
			PendingChangesMessage.IsVisible = true;
		}

		private void SixtyFPSButton_Click(object sender, RoutedEventArgs e) =>
			LatencyBox.Text = WavyBackgroundRenderer.GetFPSTimeSpan(60).TotalMilliseconds.ToString();

		private void TwelveFPSButton_Click(object sender, RoutedEventArgs e) =>
			LatencyBox.Text = WavyBackgroundRenderer.GetFPSTimeSpan(12).TotalMilliseconds.ToString();

		bool open;

		private void BG2Render_MouseLeftButtonUp(object sender, PointerPressedEventArgs e)
		{
			if (open) return;
			double width = ImageContent.Width;
			ImageContentHost.Content = null;
			//POP OUT LARGE VIEW
			var hwnd = new Window() {
				Title   = "Large Background Image Viewer",
				Content = ImageContent,
				Width   = 512,
				Height  = 512,
			};
			hwnd.SizeChanged += delegate {
				ScrWidth           = hwnd.Width;
				ScrHeight          = hwnd.Height;
				ImageContent.Width = ScrWidth;
				ResetViewSettings();
			};
			hwnd.Background = this.FindResource("WindowBackgroundColor") as IBrush;
			hwnd.Closed += delegate {
				open                     = false;
				ImageContent.Background  = null;
				ImageContentHost.Content = ImageContent;
				ImageContent.Width       = width;
			};
			open = true;
			hwnd.Show(Application.Current.MainWindow());
		}

		private void BreakdownButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(String.Join("\n", ImageContent.ReferencedFiles.Select(x => $"{x.Key}: {x.Value}")));
		}

		private void ViewOptions_BG2_ScrollValueChanged(object sender, ScrollEventArgs e)
		{
			if (IgnoreUserInput) return;
			if (e.Horizontal)
				BG2X  = e.ScrollValue;
			else BG2Y = e.ScrollValue;
			ResetViewSettings();
		}

		private void ViewOptions_BG3_ScrollValueChanged(object sender, ScrollEventArgs e)
		{
			if (IgnoreUserInput) return;
			if (e.Horizontal)
				BG3X  = e.ScrollValue;
			else BG3Y = e.ScrollValue;
			ResetViewSettings();
		}
	}
}
