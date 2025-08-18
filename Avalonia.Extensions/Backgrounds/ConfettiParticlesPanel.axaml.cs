using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Extensions.Backgrounds
{
	public partial class ConfettiParticlesPanel : UserControl
	{
		static readonly Random Rand = new Random();
		private static readonly IReadOnlyList<Point3D> Positions = BackgroundCommon.GeneratePositions(.07f);

		public ConfettiParticlesPanel()
		{
			InitializeComponent();
			BackgroundCommon.LoadScene_Confetti(100, 70, 500, Rand, null, Positions,
				Colors.Blue, Colors.Red, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.White, Colors.Green);
		}
	}
}
