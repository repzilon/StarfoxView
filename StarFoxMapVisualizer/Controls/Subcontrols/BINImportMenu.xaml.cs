﻿using System.Windows;
using System.Windows.Controls;
using StarFox.Interop.MISC;
using static StarFox.Interop.SFFileType;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for FileImportMenu.xaml
    /// </summary>
    public partial class BINImportMenu : Window
    {
        /// <summary>
        /// The selected type of file
        /// </summary>
        public BINFileTypes FileType { get; private set; }
        public BINImportMenu()
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
            foreach(var type in Utility.GetValues<BINFileTypes>())
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
        }

        /// <summary>
        /// Dismiss the window with the specified result
        /// </summary>
        /// <param name="FileType"></param>
        private void Dismiss(BINFileTypes FileType)
        {
            DialogResult = true;
            this.FileType = FileType;
            Close();
        }
    }
}
