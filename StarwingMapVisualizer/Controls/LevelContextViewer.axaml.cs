using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using StarFox.Interop.MAP.CONTEXT;

namespace StarwingMapVisualizer.Controls
{
	public partial class LevelContextViewer : Window
	{
		/// <summary>
		/// This event is raised when the "Use as Level Context" button is pressed.
		/// <para>You should update the preview of the level context with this new selection when this event is raised.</para>
		/// </summary>
		public event EventHandler<MAPContextDefinition> EditorPreviewSelectionChanged;

		public LevelContextViewer()
		{
			InitializeComponent();
			ViewBar.IsVisible  = false;
		}

		public LevelContextViewer(MAPContextDefinition levelContext) : this()
		{
			if (levelContext != null) {
				Loaded += async delegate { await Attach(levelContext); };
			}
		}

		public LevelContextViewer(MAPContextFile contextFile) : this()
		{
			if (contextFile != null) {
				Loaded += async delegate { await AttachMany(contextFile); };
			}
		}

		public LevelContextViewer(params MAPContextDefinition[] contexts) : this()
		{
			Loaded += async delegate { await AttachMany(contexts); };
		}

		public MAPContextDefinition SelectedLevelContext { get; private set; }
		public MAPContextFile SelectedFile { get; private set; }
		private MAPContextDefinition ViewSwitcherSelectionAsContext => (MAPContextDefinition)ViewSwitcher.SelectedItem;

		public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
		{
			IsEnabled            = false;
			SelectedLevelContext = levelContext;
			if (SelectedLevelContext == null) {
				IsEnabled = true;
				return;
			}

			Title = SelectedLevelContext.MapInitName;
			await LevelViewerControl.Attach(levelContext, ExtractCCR, ExtractPCR);
			IsEnabled = true;
		}

		public async Task AttachMany(MAPContextFile contextFile)
		{
			SelectedFile = contextFile;
			if (SelectedFile == null || !SelectedFile.Definitions.Any()) {
				return;
			}

			await AttachMany(contextFile.Definitions.Values.ToArray());
		}

		public async Task AttachMany(params MAPContextDefinition[] contexts)
		{
			ViewBar.IsVisible  = false;
			if (contexts.Length > 1) {
				ViewBar.IsVisible             =  true;
				ViewSwitcher.SelectionChanged -= ChangeDefinition;
				ViewSwitcher.ItemsSource      =  contexts;
				ViewSwitcher.SelectionChanged += ChangeDefinition;
				if (ViewSwitcher.ItemCount > 0) {
					ViewSwitcher.SelectedIndex = 1;
				}
			} else if (contexts.Length == 1) {
				await Attach(contexts[0]);
			}
		}

		private async void ChangeDefinition(object sender, SelectionChangedEventArgs e)
		{
			await Attach(ViewSwitcherSelectionAsContext);
		}

		private async void ReextractButton_Click(object sender, RoutedEventArgs e)
		{
			await Attach(ViewSwitcherSelectionAsContext, true, true);
		}

		private void HOST_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
		{
			// TODO : Emulate DragMove in HOST_MouseLeftButtonDown
			//DragMove();
		}

		private void HOST_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e) { }

		private void ButtonOK_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ShareButton_Click(object sender, RoutedEventArgs e)
		{
			var image = LevelViewerControl.ImageContent.BG2Render.Source as Bitmap;
			if (image != null) {
				// TODO : platform-specific code to copy image to clipboard
				//Clipboard.SetImage(image);
			}
		}

		private void UseAsButton_Click(object sender, RoutedEventArgs e)
		{
			EditorPreviewSelectionChanged?.Invoke(this, SelectedLevelContext);
		}
	}
}
