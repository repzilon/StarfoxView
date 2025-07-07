using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using StarFox.Interop.MISC;
using StarFoxMapVisualizer.Misc;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Controls
{
	/// <summary>
	/// Interaction logic for PaletteView.xaml
	/// </summary>
	public partial class PaletteView : Window
	{
		private byte[] m_bytarPhotoshopPalette;

		public PaletteView()
		{
			InitializeComponent();
			MouseLeftButtonDown += PaletteView_MouseLeftButtonDown;
		}

		private void PaletteView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

		public void SetupControl(COL Palette)
		{
			using (var bitmap = Palette.RenderPalette()) {
				PaletteViewImage.Source = bitmap.Convert();
			}

			var colarGdip = Palette.GetPalette();
			ColorsBlock.Text = colarGdip.Length.ToString();
			m_bytarPhotoshopPalette = colarGdip.ToPhotoshop();
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetImage(PaletteViewImage.Source as BitmapSource);
		}

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_bytarPhotoshopPalette != null) {
				var saveDialog = FILEStandard.InitSaveFileDialog("Export palette", "starfox.act");
				var filters = new FileDialogFilterBuilder(false);
				filters.Add("Photoshop palette", "act");
				saveDialog.Filter = filters.ToString();
				if (saveDialog.ShowDialog() == true) {
					File­.WriteAllBytes(saveDialog.FileName, m_bytarPhotoshopPalette);
				}
			}
		}
	}
}
