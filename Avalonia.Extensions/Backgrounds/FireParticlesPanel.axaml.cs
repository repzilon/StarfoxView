using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Extensions.Backgrounds
{
	public partial class FireParticlesPanel : UserControl
	{
		static readonly Random Rand = new Random();
		private static readonly IReadOnlyList<Point3D> Positions = BackgroundCommon.GeneratePositions(.03f);

		public FireParticlesPanel()
		{
			InitializeComponent();
			BackgroundCommon.LoadScene_Confetti(150, 35, 500, Rand, null, Positions,
				Colors.Red, Colors.White, Colors.Orange, Colors.Yellow);
		}
	}
}
