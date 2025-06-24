using System.Windows;
using System.Windows.Controls;
using StarFox.Interop.GFX;
using StarFox.Interop.MISC;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for FileImportMenu.xaml
    /// </summary>
    public partial class BPPDepthMenu : Window
    {
        /// <summary>
        /// The selected type of file
        /// </summary>
        public CAD.BitDepthFormats FileType { get; private set; }
        public BPPDepthMenu()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            showOptions();
        }

        private void showOptions()
        {
            TypeMenu.Items.Clear();
            foreach(var type in Utility.GetValues<CAD.BitDepthFormats>())
            {
                var item = new MenuItem()
                {
                    Header = type.ToString()
                };
                item.PreviewMouseLeftButtonUp += delegate
                {
                    Dismiss(type);
                };
                TypeMenu.Items.Add(item);
            }
            var citem = new MenuItem()
            {
                Header = "Cancel"
            };
            citem.PreviewMouseLeftButtonUp += delegate
            {
                DialogResult = false;
                Close();
            };
            TypeMenu.Items.Add(citem);
        }

        /// <summary>
        /// Dismiss the window with the specified result
        /// </summary>
        /// <param name="FileType"></param>
        private void Dismiss(CAD.BitDepthFormats FileType)
        {
            DialogResult = true;
            this.FileType = FileType;
            Close();
        }

        private void CancelItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
