using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
	/// <summary>
	/// Interaction logic for CopyableImage.xaml
	/// </summary>
	public partial class CopyableImage : Image
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

		private void ExportItem_Click(object sender, RoutedEventArgs e)
		{
			var image = Source as BitmapImage;
			if (image != null) {
				var flsImage = image.StreamSource as FileStream;
				var fileDialog = new SaveFileDialog()
				{
					AddExtension = true,
					CreatePrompt = false,
					CheckFileExists = false,
					CheckPathExists = true,
					InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName,
					Title = "Export image",
					FileName = (flsImage != null ? Path.GetFileNameWithoutExtension(flsImage.Name) : "Untitled") + ".png",
					OverwritePrompt = true
				};
				var filters = new FileDialogFilterBuilder(false);
				filters.Add("Portable Network Graphics", "png");
				filters.Add("Graphic Interchange Format", "gif");
				filters.Add("Tagged Image File Format", "tiff");
				filters.Add("Windows Bitmap", "bmp");
				fileDialog.Filter = filters.ToString();
				if (fileDialog.ShowDialog() == true) {
					var ext = Path.GetExtension(fileDialog.FileName);
					BitmapEncoder encoder;
					// TODO : Export with the full palette (BitmapEncoder class has a Palette property)
					if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)) {
						encoder = new GifBitmapEncoder();
					} else if (ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase)) {
						encoder = new TiffBitmapEncoder();
					} else if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)) {
						encoder = new BmpBitmapEncoder();
					} else {
						encoder = new PngBitmapEncoder();
					}
					encoder.Frames.Add(BitmapFrame.Create(image));

					using (var fileStream = new FileStream(fileDialog.FileName, FileMode.Create)) {
						encoder.Save(fileStream);
					}
				}
			}
		}
	}
}
