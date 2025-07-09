using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StarFoxMapVisualizer.Misc;
using Color = System.Windows.Media.Color;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
	/// <summary>
	/// Interaction logic for CopyableImage.xaml
	/// </summary>
	public partial class CopyableImage
	{
		public CopyableImage()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Copies the image to the <see cref="Clipboard"/>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CopyImage_Click(object sender, RoutedEventArgs e)
		{
			var image = Source as BitmapImage;
			if (image != null) Clipboard.SetImage(image);
			else {
				var error = new InvalidOperationException("Copying that image failed, it isn't of the correct type.\n" +
					"Probably my bad, I apologize. Let me know with a screenshot please. :)");
				AppResources.ShowCrash(error, false, "Copying/Exporting an image.");
			}
		}

		private static FrameworkElement GetAncestor(FrameworkElement descendant, int levels)
		{
			FrameworkElement ancestor = descendant;
			for (int i = 1; i <= levels; i++) {
				ancestor = (FrameworkElement)ancestor.Parent;
			}

			return ancestor;
		}

		private void ExportItem_Click(object sender, RoutedEventArgs e)
		{
			if (Source is BitmapImage image) {
				var ctlGFX = GetAncestor(this, 4) as GFXControl;
				string strImagePath = null;
				bool blnSaveFullPalette = false;
				if (ctlGFX != null) { // GFXControl is an ancestor of the image on canvas
					strImagePath = ctlGFX.SelectedGraphic;
					blnSaveFullPalette = true;
				} else {
					ctlGFX = GetAncestor(this, 6) as GFXControl;
					if (ctlGFX != null) { // GFXControl is an ancestor of the palette swatches 
						var lvi = ctlGFX.PaletteSelection.SelectedItem as ListViewItem;
						if (lvi?.Tag is PaletteTuple tuple) {
							strImagePath = tuple.Name;
						}
					} else if (image.StreamSource is FileStream flsImage) {
						strImagePath = flsImage.Name;
					}
				}

				var fileDialog = FILEStandard.InitSaveFileDialog("Export image",
					(strImagePath != null ? Path.GetFileNameWithoutExtension(strImagePath) : "Untitled") + ".png");
				var filters = new FileDialogFilterBuilder(false);
				filters.Add("Portable Network Graphics", "png");
				filters.Add("Graphic Interchange Format", "gif");
				filters.Add("Tagged Image File Format", "tiff");
				filters.Add("Windows Bitmap", "bmp");
				fileDialog.Filter = filters.ToString();
				if (fileDialog.ShowDialog() == true) {
					var ext = Path.GetExtension(fileDialog.FileName);
					BitmapEncoder encoder;
					if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)) {
						encoder = new GifBitmapEncoder();
					} else if (ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase)) {
						encoder = new TiffBitmapEncoder();
					} else if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)) {
						encoder = new BmpBitmapEncoder();
					} else {
						encoder = new PngBitmapEncoder();
					}

					BitmapFrame frame;
					if (blnSaveFullPalette) {
						// Setting the palette to the image encoder does not work.
						// Convert the image to indexed color, right before encoding it.
						var lstColors = ctlGFX.SelectedPalette.GetPalette().Select(ToWPFColor).ToList();
						frame = BitmapFrame.Create(new FormatConvertedBitmap(new CroppedBitmap(image, image.FindImageRectangle(2)),
							PixelFormats.Indexed8, new BitmapPalette(lstColors), 0));
					} else {
						frame = BitmapFrame.Create(image);
					}
					encoder.Frames.Add(frame);

					using (var fileStream = new FileStream(fileDialog.FileName, FileMode.Create)) {
						encoder.Save(fileStream);
					}
				}
			}
		}

		private static Color ToWPFColor(System.Drawing.Color x)
		{
			return Color.FromArgb(x.A, x.R, x.G, x.B);
		}
	}
}
