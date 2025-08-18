using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Extensions.Backgrounds
{
	public partial class SnowFallPanel : UserControl
	{
		internal static Random SnowFallRandom = new Random();

		public double SPEED;
		public int TileSize;
		List<Image> SnowControls = new List<Image>();

		public SnowFallPanel() : this(TimeSpan.Zero) { }

		public SnowFallPanel(TimeSpan animationDelay, int tileSize = 75, double speed = 5)
		{
			if (animationDelay == default) {
				animationDelay = TimeSpan.FromSeconds(0);
			}

			SPEED = speed;
			InitializeComponent();
			Loaded += delegate {
				// TODO : Correct TiledBG.Viewport
				//TiledBG.Viewport = new Rect(0, 0, tileSize, tileSize);
				if (animationDelay.TotalSeconds != 0) {
					var task = Task.Delay(animationDelay.Milliseconds);
					// TODO : Invoke init
					//task.ContinueWith(delegate { Dispatcher.Invoke(Init); });
					return;
				}

				Init();
			};
		}

		private void Init()
		{
			var UNIF_VAL = Math.Max(SnowGrid.Width, SnowGrid.Height);
			// TODO : Begin translation animation
			//TRANSLATION.BeginAnimation(TranslateTransform.XProperty, GetBackgroundAnimation(0, -(int)UNIF_VAL, SPEED));
			//TRANSLATION.BeginAnimation(TranslateTransform.YProperty, GetBackgroundAnimation(0, (int)UNIF_VAL, SPEED));
			SnowGrid.Margin = new Thickness(0, -UNIF_VAL, -UNIF_VAL, 0);
		}

		/* TODO : Enable GetBackgroundAnimation
		private DoubleAnimation GetBackgroundAnimation(int fromValue, int toValue, double time)
		{
			return new DoubleAnimation(fromValue, toValue, TimeSpan.FromSeconds(time)) {
				RepeatBehavior = RepeatBehavior.Forever
			};
		}// */

		#region PERFORMANCE DISASTER
		/* TODO : Support GetAnimationTimeline
		private DoubleAnimationUsingPath GetAnimationTimeline(Point startPoint, Point endpoint)
		{
			DoubleAnimationUsingPath timeline = new DoubleAnimationUsingPath();
			PathFigure figure = new PathFigure() {
				StartPoint = startPoint,
				IsClosed   = false
			};
			timeline.PathGeometry = new PathGeometry();

			figure.Segments.Add(
				new BezierSegment {
					Point1    = new Point(startPoint.X - 25, startPoint.Y + 100),
					Point2    = new Point(startPoint.X, startPoint.Y + 300),
					Point3    = endpoint,
					IsStroked = false
				});
			timeline.PathGeometry.Figures.Add(figure);
			return timeline;
		}// */

		/* TODO : Support ApplyAnimation
		private void ApplyAnimation(Image control, Point startPoint)
		{
			var transform = new TranslateTransform();
			RegisterName("snowPieceTransform", transform);
			control.RenderTransform = transform;
			int far = (int)SnowGrid.Height;
			if (far == 0) {
				far = 700;
			}

			Point endpoint = new Point(startPoint.X - 75, far + 75);
			Storyboard anim = new Storyboard() {
				Duration = TimeSpan.FromSeconds(5),
			};
			var xTimeline = GetAnimationTimeline(startPoint, endpoint);
			xTimeline.Source = PathAnimationSource.X;
			Storyboard.SetTargetName(xTimeline, "snowPieceTransform");
			Storyboard.SetTargetProperty(xTimeline, new PropertyPath(TranslateTransform.XProperty));
			xTimeline.Freeze();
			anim.Children.Add(xTimeline);
			xTimeline        = GetAnimationTimeline(startPoint, endpoint);
			xTimeline.Source = PathAnimationSource.Y;
			Storyboard.SetTargetName(xTimeline, "snowPieceTransform");
			Storyboard.SetTargetProperty(xTimeline, new PropertyPath(TranslateTransform.YProperty));
			xTimeline.Freeze();
			anim.Children.Add(xTimeline);
			anim.Begin(control, false);
		}// */

		private Image GetSnowControl(out Point location)
		{
			Image snowControl = new Image();
			SnowGrid.Children.Add(snowControl);
			//get random location on the top edge of the control
			int far = (int)SnowGrid.Width;
			if (far == 0) {
				far = 1000;
			}

			location           = new Point(SnowFallRandom.Next(0, far), -50);
			snowControl.Margin = new Thickness(location.X, location.Y, 0, 0);
			return snowControl;
		}
		#endregion
	}
}
