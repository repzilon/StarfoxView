using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Extensions.Backgrounds
{
	public partial class RainParticlesPanel : UserControl
	{
		static readonly Random Rand = new Random();
		private static readonly IReadOnlyList<Point3D> Positions = BackgroundCommon.GeneratePositions(.07f);

		public RainParticlesPanel()
		{
			InitializeComponent();
			BackgroundCommon.LoadScene_Confetti(200, 50, 500, Rand, null, Positions,
				Colors.Blue, Colors.LightBlue, Colors.SkyBlue, Colors.LightSkyBlue, Colors.DeepSkyBlue);
		}
	}
}
