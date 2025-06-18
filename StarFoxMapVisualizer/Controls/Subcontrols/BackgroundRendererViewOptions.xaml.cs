using System;
using System.Windows;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for BackgroundRendererViewOptions.xaml
    /// </summary>
    public partial class BackgroundRendererViewOptions : HeaderedContentControl
    {
        public event EventHandler<ScrollEventArgs> BG2_ScrollValueChanged, BG3_ScrollValueChanged;

        private void XScrollSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BG3_ScrollValueChanged?.Invoke(this, new ScrollEventArgs(sender == XScrollSlider, e.NewValue));
        }

        private void XScrollSlider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BG2_ScrollValueChanged?.Invoke(this, new ScrollEventArgs(sender == XScrollSlider2, e.NewValue));
        }

        public BackgroundRendererViewOptions()
        {
            InitializeComponent();
        }
    }
}
