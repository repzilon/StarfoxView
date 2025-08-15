using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaloniaPanNZoom.CustomControls
{
	public interface IDraggable
	{
		bool Draggable { get; }
	}

	public partial class PanAndZoomCanvas : Canvas
	{
		private static PanAndZoomCanvas _currentlyDragging;
		private static bool Dragging;

		#region Variables
		private readonly MatrixTransform _transform = new MatrixTransform();
		private Point _initialMousePosition;

		private bool _dragging;
		private Control _selectedElement;
		private Vector _draggingDelta;

		private Color _lineColor = Colors.Green;
		private Color _backgroundColor = Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
		private readonly List<Line> _gridLines = new List<Line>();
		#endregion

		public event EventHandler<Point> LocationChanged;

		/// <summary>
		/// The current position of the camera in this virtual 2D space
		/// </summary>
		public Point Location { get; private set; }

		/// <summary>
		/// When true, only the view can be moved. No components inside can be moved when this is ON.
		/// </summary>
		public bool ViewMode { get; set; } = true;

		public PanAndZoomCanvas()
		{
			InitializeComponent();

			PointerPressed      += PanAndZoomCanvas_MouseDown;
			PointerReleased     += PanAndZoomCanvas_MouseUp;
			PointerMoved        += PanAndZoomCanvas_MouseMove;
			PointerWheelChanged += PanAndZoomCanvas_MouseWheel;

			BackgroundColor = _backgroundColor;
			// draw lines
			for (int x = -4000; x <= 4000; x += 100) {
				Line verticalLine = new Line {
					Stroke          = new SolidColorBrush(_lineColor),
					StartPoint      = new Point(x, -4000),
					EndPoint        = new Point(x, 4000),
					StrokeThickness = x % 1000 == 0 ? 6 : 2
				};

				Children.Add(verticalLine);
				_gridLines.Add(verticalLine);
			}

			for (int y = -4000; y <= 4000; y += 100) {
				Line horizontalLine = new Line {
					Stroke          = new SolidColorBrush(_lineColor),
					StartPoint      = new Point(-4000, y),
					EndPoint        = new Point(4000, y),
					StrokeThickness = y % 1000 == 0 ? 6 : 2
				};

				Children.Add(horizontalLine);
				_gridLines.Add(horizontalLine);
			}
		}

		public const float ZoomFactor = 1.1f;

		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				foreach (Line line in _gridLines) {
					line.Stroke = new SolidColorBrush(_lineColor);
				}
			}
		}

		public Color BackgroundColor
		{
			get { return _backgroundColor; }
			set
			{
				_backgroundColor = value;
				Background       = new SolidColorBrush(_backgroundColor);
			}
		}

		public void SetGridVisibility(bool value)
		{
			foreach (Line line in _gridLines) {
				line.IsVisible = value;
			}
		}

		private void PanAndZoomCanvas_MouseDown(object sender, PointerPressedEventArgs e)
		{
			if (e.Handled) return;
			if (Dragging) return;
			if (e.Properties.IsLeftButtonPressed) {
				e.Handled             = true;
				_initialMousePosition = _transform.Inverse().Transform(e.GetPosition(this));
				Dragging              = true;
				_currentlyDragging    = this;
			}

			if (e.Properties.IsRightButtonPressed) {
				if (Children.Contains((Control)e.Source)) {
					_selectedElement = (Control)e.Source;
					if (_selectedElement is IDraggable draggable && draggable.Draggable) {
						var mousePosition   = e.GetCurrentPoint(sender as Control);
						var x               = GetLeft(_selectedElement);
						var y               = GetTop(_selectedElement);
						var elementPosition = new Point(x, y);
						_draggingDelta = elementPosition - mousePosition.Position;
					} else return;
				}

				_dragging = true;
			}
		}

		private void PanAndZoomCanvas_MouseUp(object sender, PointerReleasedEventArgs e)
		{
			_dragging        = false;
			_selectedElement = null;
			Dragging         = false;
		}

		public void MoveCanvas(Vector offset)
		{
			var translate = new TranslateTransform(offset.X, offset.Y);
			_transform.Matrix =  translate.Value * _transform.Matrix;
			Location          -= offset;

			foreach (var child in Children) {
				child.RenderTransform = _transform;
			}

			LocationChanged?.Invoke(this, Location);
		}

		public void MoveCanvas(Point offset) => MoveCanvas(new Vector(offset.X, offset.Y));

		public void SetCanvasLocation(Point location)
		{
			this.MoveCanvas((Vector)(this.Location - location));
		}

		private void PanAndZoomCanvas_MouseMove(object sender, PointerEventArgs e)
		{
			if (e.Handled) return;
			if (!Dragging) return;
			if (_currentlyDragging != this) return;
			if (e.Properties.IsLeftButtonPressed) {
				e.Handled = true;
				Point  mousePosition = _transform.Inverse().Transform(e.GetPosition(this));
				Vector delta         = mousePosition - _initialMousePosition;
				MoveCanvas(delta);
			}

			if (_dragging && e.Properties.IsRightButtonPressed) {
				var point = e.GetCurrentPoint(sender as Control);
				var x     = point.Position.X;
				var y     = point.Position.Y;

				if (_selectedElement != null) {
					SetLeft(_selectedElement, x + _draggingDelta.X);
					SetTop(_selectedElement, y + _draggingDelta.Y);
				}
			}
		}

		public void ZoomInverseOnce(float scaleFactor = ZoomFactor) => ZoomOnce(1f / scaleFactor);

		[Obsolete("Not really obsolete, but its implementation is incomplete. Avoid if possible for now.")]
		public void ZoomOnce(float scaleFactor = ZoomFactor)
		{
			// TODO : Detect platform and invoke platform API to query mouse position
			// See https://github.com/AvaloniaUI/Avalonia/discussions/6386
			Point mousePosition = new Point(0, 0); //Mouse.GetPosition(this);

			Matrix scaleMatrix = _transform.Matrix;
			scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePosition.X, mousePosition.Y);
			_transform.Matrix = scaleMatrix;

			foreach (var child in Children) {
				double x = GetLeft(child);
				double y = GetTop(child);

				double sx = x * scaleFactor;
				double sy = y * scaleFactor;

				SetLeft(child, sx);
				SetTop(child, sy);

				child.RenderTransform = _transform;
			}
		}

		private void PanAndZoomCanvas_MouseWheel(object sender, PointerWheelEventArgs e)
		{
			if (e.Handled) return;
			e.Handled = true;
			float scaleFactor = ZoomFactor;
			if (e.Delta.Length < 0) {
				scaleFactor = 1f / scaleFactor;
			}

			ZoomOnce(scaleFactor);
		}

		public void Reset()
		{
			foreach (var child in Children) {
				child.RenderTransform = null;
			}
		}
	}

	internal static class Bandaid
	{
		internal static Matrix Inverse(this MatrixTransform avaloniaMtx)
		{
			return avaloniaMtx.Value.Invert();
		}

		#region From https://github.com/wieslawsoltes/PanAndZoom/blob/master/src/PanAndZoom/MatrixHelper.cs
		// Both https://github.com/SEilers/WpfPanAndZoom (base of this project) and
		// https://github.com/wieslawsoltes/PanAndZoom (code below) are under the MIT license,
		// so copying code in any direction is not a problem.

		/// <summary>
		/// Creates a matrix that is scaling from a specified center.
		/// </summary>
		/// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
		/// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
		/// <param name="centerX">The center X-coordinate of the scaling.</param>
		/// <param name="centerY">The center Y-coordinate of the scaling.</param>
		/// <returns>The created scaling matrix.</returns>
		internal static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
		{
			return new Matrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
		}

		/// <summary>
		/// Prepends a scale around the center of provided matrix.
		/// </summary>
		/// <param name="matrix">The matrix to prepend scale.</param>
		/// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
		/// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
		/// <param name="centerX">The center X-coordinate of the scaling.</param>
		/// <param name="centerY">The center Y-coordinate of the scaling.</param>
		/// <returns>The created scaling matrix.</returns>
		internal static Matrix ScaleAt(this Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
		{
			return ScaleAt(scaleX, scaleY, centerX, centerY) * matrix;
		}
		#endregion
	}
}
