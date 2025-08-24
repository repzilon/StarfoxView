using Avalonia.Controls;
using Avalonia.Interactivity;
using StarFox.Interop.MISC;
using static StarFox.Interop.SFFileType;

namespace StarwingMapVisualizer.Controls.Subcontrols
{
	public partial class FileImportMenu : Window
	{
		/// <summary>
		/// The selected type of file
		/// </summary>
		public ASMFileTypes FileType { get; private set; }

		public FileImportMenu()
		{
			InitializeComponent();

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			TypeMenu.Children.Clear();
			foreach (var type in Utility.GetValues<ASMFileTypes>()) {
				var item = new Button() {
					Content = GetSummary(type)
				};
				item.Click += delegate { Dismiss(type); };
				TypeMenu.Children.Add(item);
			}

			var citem = new Button() {
				Content = "Cancel",
				IsCancel = true
			};
			citem.Click += delegate { Close(false); };
			TypeMenu.Children.Add(citem);
			Activate();
		}

		/// <summary>
		/// Dismiss the window with the specified result
		/// </summary>
		/// <param name="fileType"></param>
		private void Dismiss(ASMFileTypes fileType)
		{
			this.FileType = fileType;
			Close(true);
		}
	}
}
