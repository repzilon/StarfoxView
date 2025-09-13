using System;
using Avalonia.Controls.Primitives;

namespace StarwingMapVisualizer.Controls.Subcontrols
{
	public partial class BackgroundRendererViewOptions : HeaderedContentControl
	{
		public event EventHandler<ScrollEventArgs> BG2_ScrollValueChanged, BG3_ScrollValueChanged;

		private void XScrollSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			BG3_ScrollValueChanged?.Invoke(this, new ScrollEventArgs(sender == XScrollSlider, e.NewValue));
		}

		private void XScrollSlider2_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			BG2_ScrollValueChanged?.Invoke(this, new ScrollEventArgs(sender == XScrollSlider2, e.NewValue));
		}

		public BackgroundRendererViewOptions()
		{
			InitializeComponent();
		}
	}
}
