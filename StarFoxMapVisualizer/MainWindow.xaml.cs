using System.Windows;

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
            return;            
        }
    }
}
