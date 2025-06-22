using System.Windows;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for PaletteViewWindow.xaml
    /// </summary>
    public partial class PaletteSelectionWindow : Window
    {
        public PaletteTuple? SelectedPalette => PaletteSelection.SelectedPalette;

        public PaletteSelectionWindow()
        {
            InitializeComponent();
            Loaded += PaletteSelectionWindow_Loaded;
        }

        private void PaletteSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PaletteSelection.InvalidatePalettes();
        }

        private void PaletteSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaletteSelection.SelectedItem == default) return;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
