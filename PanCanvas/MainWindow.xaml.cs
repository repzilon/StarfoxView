using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfPanAndZoom
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CustomControls.Widget w1 = new CustomControls.Widget
            {
                Width = 200,
                Height = 150
            };
            canvas.Children.Add(w1);
            w1.Header.Text = "Widget 1";
            Canvas.SetTop(w1, 100);
            Canvas.SetLeft(w1, 100);

            CustomControls.Widget w2 = new CustomControls.Widget
            {
                Width = 200,
                Height = 150
            };
            canvas.Children.Add(w2);
            w2.Header.Text = "Widget 2";
            w2.HeaderRectangle.Fill = Brushes.Blue;
            Canvas.SetTop(w2, 400);
            Canvas.SetLeft(w2, 400);

            CustomControls.Widget w3 = new CustomControls.Widget
            {
                Width = 200,
                Height = 150
            };
            canvas.Children.Add(w3);
            w3.Header.Text = "Widget 3";
            w3.HeaderRectangle.Fill = Brushes.Red;
            Canvas.SetTop(w3, 400);
            Canvas.SetLeft(w3, 800);
        }
    }
}
