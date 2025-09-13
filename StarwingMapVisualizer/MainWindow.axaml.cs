using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using StarwingMapVisualizer.Controls;
using StarwingMapVisualizer.Dialogs;
using StarwingMapVisualizer.Misc;
using StarwingMapVisualizer.Screens;

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

		private void OnLoad(object sender, EventArgs e)
		{
			Title = AppResources.GetTitleLabel;
			EDITORStandard.ShowNotification("Welcome to SFView!");
		}

		internal void PushNotification(Notification notification)
		{
			_notificationManager.Show(notification);
		}

		#region Native menu bar
		/// <summary>
		/// Prompts the user to export all 3D models and will export them
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ExportAll3DButton_Click(object sender, EventArgs e) =>
			await EDITORStandard.Editor_ExportAll3DShapes(".sfshape");

		private async void ExportAll3DObjButton_Click(object sender, EventArgs e) =>
			await EDITORStandard.Editor_ExportAll3DShapes(".obj");

		/// <summary>
		/// Opens the Level Background viewer dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BGSASMViewerButton_Click(object sender, EventArgs e)
		{
			var file = FILEStandard.MAPImport?.LoadedContextDefinitions;
			if (file == null) {
				MessageBox.Show(
					"Level contexts have not been loaded yet. Open a level file to have this information populated.");
				return;
			}

			LevelContextViewer viewer = new LevelContextViewer(file) {
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			viewer.Show(Application.Current.MainWindow());
		}

		private void CloseProjectMenuItem_Click(object sender, EventArgs e)
		{
			//Delete old project
			AppResources.ImportedProject = null;
			//switch to landing screen
			((MainWindow)Application.Current.MainWindow()).Content = new LandingScreen();
		}

		/// <summary>
		/// Fired when the Level Select Menu item is selected
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LevelSelectItem_Click(object sender, EventArgs e)
		{
			// TODO : Import LevelSelectWindow
			//var wnd = new LevelSelectWindow();
			//wnd.Show();
		}

		private void OpenProjectFolderItem_Click(object sender, EventArgs e)
		{
			OpenExternal.Folder(AppResources.ImportedProject.WorkspaceDirectory.FullName, true);
		}

		private async void ConvertSfscreenItem_OnClick(object sender, EventArgs e)
		{
			await GFXStandard.ConvertFromSfscreen();
		}
		#endregion
	}
}
