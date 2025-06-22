using System;
using System.Windows;
using System.Windows.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //**CAD SET CONTEXT
            StarFox.Interop.GFX.CAD.CGX.GlobalContext.HandlePaletteIndex0AsTransparent = true;
            //**

            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            Title = AppResources.GetTitleLabel;
            await EDITORStandard.ShowNotification("Welcome to SFView!", delegate { }, TimeSpan.FromSeconds(5));
            return;
        }

        internal void PushNotification(Notification Notification)
        {
            var obj = (ContentControl)Template.FindName("UI_PARENT_NOTIFICATION", this);
            obj.Content = Notification;
            Notification.Show();
            Notification.Dismissed += delegate
            {
                obj.Content = null;
            };
        }
    }
}
