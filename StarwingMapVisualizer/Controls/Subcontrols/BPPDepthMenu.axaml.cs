using Avalonia.Controls;
using Avalonia.Interactivity;
using StarFox.Interop.GFX;
using StarFox.Interop.MISC;

namespace StarwingMapVisualizer.Controls.Subcontrols
{
	public partial class BPPDepthMenu : Window
	{
		/// <summary>
		/// The selected type of file
		/// </summary>
		public CAD.BitDepthFormats FileType { get; private set; }

		public BPPDepthMenu()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			TypeMenu.Children.Clear();
			foreach (var type in Utility.GetValues<CAD.BitDepthFormats>()) {
				var item = new MenuItem() {
					Header = type.ToString()
				};
				item.Click += delegate { Dismiss(type); };
				TypeMenu.Children.Add(item);
			}

			var citem = new MenuItem() {
				Header = "Cancel"
			};
			citem.Click += delegate { Close(false); };
			TypeMenu.Children.Add(citem);
		}

		/// <summary>
		/// Dismiss the window with the specified result
		/// </summary>
		/// <param name="fileType"></param>
		private void Dismiss(CAD.BitDepthFormats fileType)
		{
			this.FileType = fileType;
			Close(true);
		}

		private void CancelItem_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
