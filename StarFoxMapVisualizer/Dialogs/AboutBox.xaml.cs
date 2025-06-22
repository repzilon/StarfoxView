using System.Diagnostics;
using System.Windows;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void GithubLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", "https://github.com/JDrocks450");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
