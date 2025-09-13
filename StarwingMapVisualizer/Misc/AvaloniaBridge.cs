using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using SkiaSharp;

namespace StarwingMapVisualizer.Misc
{
	internal static class AvaloniaBridge
	{
		public static Window MainWindow(this Application avaloniaApp)
		{
			var desktop = avaloniaApp.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			return desktop?.MainWindow;
		}

		private static TopLevel OurTopLevel()
		{
			return TopLevel.GetTopLevel(Application.Current.MainWindow());
		}

		public static IClipboard Clipboard()
		{
			return OurTopLevel().Clipboard;
		}

		public static Task<IReadOnlyList<IStorageFile>> ShowDialogAsync(this FilePickerOpenOptions options,
		string initialDirectory = null)
		{
			var sp = OurTopLevel().StorageProvider;
			if (!String.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory)) {
				options.SuggestedStartLocation = sp.TryGetFolderFromPathAsync(initialDirectory).Result;
			}
			return sp.OpenFilePickerAsync(options);
		}

		public static Task<IStorageFile> ShowDialogAsync(this FilePickerSaveOptions options,
		string initialDirectory = null)
		{
			var sp = OurTopLevel().StorageProvider;
			var strStartDir = !String.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory)
				? initialDirectory
				: AppResources.ImportedProject.WorkspaceDirectory.FullName;
			options.SuggestedStartLocation = sp.TryGetFolderFromPathAsync(strStartDir).Result;
			return sp.SaveFilePickerAsync(options);
		}

		// https://github.com/AvaloniaUI/Avalonia/discussions/5908
		public static Bitmap Convert(this SKBitmap bitmap) {
			if (bitmap == null) return null;

			return new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul, bitmap.GetPixels(),
				new PixelSize(bitmap.Width, bitmap.Height), new Vector(96, 96), bitmap.RowBytes);
		}
	}
}
