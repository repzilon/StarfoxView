using System;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace StarwingMapVisualizer.Dialogs
{
	// These are values from WPF. WinForms equivalent has more values
	internal enum MessageBoxButton : byte
	{
		OK = 0,
		OKCancel = 1,
		YesNoCancel = 3,
		YesNo = 4
	}

	// These are values from WPF. WinForms DialogResult has more values
	internal enum MessageBoxResult : byte
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7
	}

	/// <summary>
	/// Bridge between MessageBox.Avalonia package and Microsoft message box API
	/// </summary>
	internal static class MessageBox
	{
		public static async Task<MessageBoxResult> Show(string message, string caption, MessageBoxButton buttons = MessageBoxButton.OK)
		{
			var box = MessageBoxManager.GetMessageBoxStandard(caption, message, Convert(buttons));
			var baseResult = await box.ShowAsync();
			return Convert(baseResult);
		}

		public static async Task Show(string message)
		{
			await Show(message, AppResources.ApplicationName);
		}

		private static MessageBoxResult Convert(ButtonResult packageResult)
		{
			if (packageResult == ButtonResult.None) {
				return MessageBoxResult.None;
			} else if (packageResult == ButtonResult.Ok) {
				return MessageBoxResult.OK;
			} else if (packageResult == ButtonResult.Cancel) {
				return MessageBoxResult.Cancel;
			} else if (packageResult == ButtonResult.Yes) {
				return MessageBoxResult.Yes;
			} else if (packageResult == ButtonResult.No) {
				return MessageBoxResult.No;
			} else {
				throw new NotSupportedException();
			}
		}

		private static ButtonEnum Convert(MessageBoxButton wpfButton)
		{
			if (wpfButton == MessageBoxButton.OK) {
				return ButtonEnum.Ok;
			} else if (wpfButton == MessageBoxButton.OKCancel) {
				return ButtonEnum.OkCancel;
			} else if (wpfButton == MessageBoxButton.YesNo) {
				return ButtonEnum.YesNo;
			} else if (wpfButton == MessageBoxButton.YesNoCancel) {
				return ButtonEnum.YesNoCancel;
			} else {
				throw new NotSupportedException();
			}
		}
	}
}
