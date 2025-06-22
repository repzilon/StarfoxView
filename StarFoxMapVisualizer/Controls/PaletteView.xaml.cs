using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using StarFoxMapVisualizer.Misc;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for PaletteView.xaml
    /// </summary>
    public partial class PaletteView : Window
    {
        public PaletteView()
        {
            InitializeComponent();
            MouseLeftButtonDown += PaletteView_MouseLeftButtonDown;
        }

        private void PaletteView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        public void SetupControl(COL Palette)
        {
            using (var _bitmap = Palette.RenderPalette())
                PaletteViewImage.Source = _bitmap.Convert();
            ColorsBlock.Text = Palette.GetPalette().Length.ToString();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(PaletteViewImage.Source as BitmapSource);
        }
    }
}
