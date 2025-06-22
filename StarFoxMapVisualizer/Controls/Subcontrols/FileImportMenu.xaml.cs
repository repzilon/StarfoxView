using System.Windows;
using System.Windows.Controls;
using Starfox.Editor;
using static StarFox.Interop.SFFileType;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for FileImportMenu.xaml
    /// </summary>
    public partial class FileImportMenu : Window
    {
        /// <summary>
        /// The selected type of file
        /// </summary>
        public ASMFileTypes FileType { get; private set; }
        public FileImportMenu()
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
            foreach(var type in SFCodeProject.GetValues<ASMFileTypes>())
            {
                var item = new MenuItem()
                {
                    Header = GetSummary(type)
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
            Activate();
        }

        /// <summary>
        /// Dismiss the window with the specified result
        /// </summary>
        /// <param name="FileType"></param>
        private void Dismiss(ASMFileTypes FileType)
        {
            DialogResult = true;
            this.FileType = FileType;
            Close();
        }
    }
}
