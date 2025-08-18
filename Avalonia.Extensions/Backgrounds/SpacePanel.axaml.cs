using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.Extensions.Backgrounds
{
	public partial class SpacePanel : UserControl
	{
		static readonly Random Rand = new Random();
		private static readonly IReadOnlyList<Point3D> Positions = BackgroundCommon.GeneratePositions(.02f);

		public SpacePanel()
		{
			InitializeComponent();
			const int kDepth = 200;
			BackgroundCommon.LoadScene_Space(kDepth, 20, kDepth * 2, Rand, null, Positions,
				Colors.White, Colors.White, Colors.White, Colors.DarkCyan, Colors.DeepSkyBlue);
		}
	}
}
