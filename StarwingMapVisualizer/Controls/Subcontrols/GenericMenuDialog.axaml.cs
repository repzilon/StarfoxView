using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StarwingMapVisualizer.Controls.Subcontrols
{
	public partial class GenericMenuDialog : Window
	{
		private readonly string[] _selections;
		public int Selection { get; private set; } = -1;
		public string SelectedItem => _selections[Selection];

		public GenericMenuDialog()
		{
			InitializeComponent();

			Loaded += OnLoaded;
		}

		public GenericMenuDialog(string caption, string message, params string[] selections) : this()
		{
			Title          = caption;
			BlurbText.Text = message;
			_selections = selections;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			SelectionMenu.Children.Clear();
			int index = -1;
			foreach (var selection in _selections) {
				index++;
				var item = new MenuItem() {
					Header = selection,
					Tag    = index
				};
				item.Click += delegate {
					int selectIndex = (int)item.Tag;
					Dismiss(selectIndex);
				};
				SelectionMenu.Children.Add(item);
			}

			var citem = new MenuItem() {
				Header = "Nevermind"
			};
			citem.Click += delegate {
				Close(false);
			};
			SelectionMenu.Children.Add(citem);

			Activate();
		}

		/// <summary>
		/// Dismiss the window with the specified result
		/// </summary>
		private void Dismiss(int index)
		{
			Selection = index;
			Close(true);
		}
	}
}
