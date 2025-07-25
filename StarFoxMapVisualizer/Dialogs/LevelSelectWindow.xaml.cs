﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StarFoxMapVisualizer.Controls;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for LevelSelectWindow.xaml
    /// </summary>
    public partial class LevelSelectWindow : Window
    {
        public LevelSelectWindow()
        {
            InitializeComponent();
        }

        private void ResoItemSelected(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                string str = item?.Header?.ToString() ?? "512x512";
                var sqSizeStr = str.Substring(0,str.IndexOf('x'));
                if (int.TryParse(sqSizeStr, out var sqSize))
                {
                    Width = sqSize;
                    Height = sqSize + MenuStrip.Height + 23;
                }
            }
        }

        bool SelectPalette(out string PaletteName)
        {
            PaletteName = null;

            PaletteSelectionWindow palWnd = new PaletteSelectionWindow();
            palWnd.ShowDialog();

            PaletteTuple? selectedItem = palWnd.SelectedPalette;
            if (selectedItem is null) return false;
            PaletteName = System.IO.Path.GetFileNameWithoutExtension(selectedItem.Value.Name);
            return true;
        }

        /// <summary>
        /// CHANGES BOTH PALETTES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PaletteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectPalette(out string PaletteName) || string.IsNullOrWhiteSpace(PaletteName)) return;
            MapViewer.SetGraphics(PaletteName, PaletteName, MapViewer.MapBackgroundName);
        }

        private void PlanetPaletteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectPalette(out string PaletteName) || string.IsNullOrWhiteSpace(PaletteName)) return;
            MapViewer.GraphicsPalette = PaletteName;
        }

        private void BGPaletteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectPalette(out string PaletteName) || string.IsNullOrWhiteSpace(PaletteName)) return;
            MapViewer.MapPalette = PaletteName;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
