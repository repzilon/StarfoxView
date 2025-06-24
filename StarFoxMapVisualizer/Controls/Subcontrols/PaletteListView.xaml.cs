using System.IO;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for PaletteListView.xaml
    /// </summary>
    public partial class PaletteListView : ListView
    {
        public PaletteTuple? SelectedPalette
        {
            get
            {
                var lviSelected = SelectedItem as ListViewItem;
                if (lviSelected != null) {
                    var objTag = lviSelected.Tag;
                    if (objTag is PaletteTuple) {
                        return (PaletteTuple)objTag;
                    }
                }
                return null;
            }
        }

        public PaletteListView()
        {
            InitializeComponent();
        }

        public void InvalidatePalettes()
        {
            Items.Clear();
            var COLFiles = AppResources.ImportedProject?.Palettes;
            if (COLFiles == null) return;
            foreach (var col in COLFiles)
            {
                var item = new ListViewItem()
                {
                    Content = Path.GetFileNameWithoutExtension(col.Key),
                    Tag = new PaletteTuple(col.Key, col.Value),
                    ToolTip = col.Key
                };
                Items.Add(item);
                if (col.Value == SelectedPalette?.Palette)
                    SelectedItem = item;
            }
        }
    }
}
