using System;
using System.Windows;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for SPCInformationDialog.xaml
    /// </summary>
    public partial class SPCInformationDialog : Window
    {
        public Object SelectedObject { get; private set; }
        public SPCInformationDialog()
        {
            InitializeComponent();
        }
        public SPCInformationDialog(Object SelectedObject) : this()
        {
            this.SelectedObject = SelectedObject;
            Loaded += OnLoad;
        }

        public void DisplayProperties(object Object)
        {
            PropViewer.Attach(Object);
            SelectedObject = Object;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            DisplayProperties(SelectedObject);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            PropViewer.ApplyValues();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
            Close();
        }
    }
}
