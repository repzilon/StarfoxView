using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using StarwingMapVisualizer;
using StarwingMapVisualizer.Misc;

namespace StarwingMapVisualizer
{
	public partial class MainWindow : Window
	{
		private readonly WindowNotificationManager _notificationManager;

		public MainWindow()
		{
			InitializeComponent();

			//**CAD SET CONTEXT
			StarFox.Interop.GFX.CAD.CGX.GlobalContext.HandlePaletteIndex0AsTransparent = true;
			//**

			Loaded += OnLoad;

			// https://github.com/AvaloniaUI/Avalonia/issues/5442
			_notificationManager = new WindowNotificationManager(this)
			{
				Position = NotificationPosition.TopCenter,
				MaxItems = 1
			};
		}

		private void OnLoad(object sender, RoutedEventArgs e)
		{
			Title = AppResources.GetTitleLabel;
			EDITORStandard.ShowNotification("Welcome to SFView!");
		}

		internal void PushNotification(Notification notification)
		{
			_notificationManager.Show(notification);
		}
	}
}
