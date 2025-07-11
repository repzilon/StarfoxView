﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfPanAndZoom.CustomControls
{
	public interface IDraggable
    {
        bool Draggable { get; }
    }

    /// <summary>
    /// Interaktionslogik für PanAndZoomCanvas.xaml
    /// https://stackoverflow.com/questions/35165349/how-to-drag-rendertransform-with-mouse-in-wpf
    /// </summary>
    public partial class PanAndZoomCanvas : Canvas
    {
        static PanAndZoomCanvas currentlyDragging = null;
        static bool Dragging = false;

        #region Variables
        private readonly MatrixTransform _transform = new MatrixTransform();
        private Point _initialMousePosition;

        private bool _dragging;
        private UIElement _selectedElement;
        private Vector _draggingDelta;

        private Color _lineColor = Colors.Green;
        private Color _backgroundColor = Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
        private List<Line> _gridLines = new List<Line>();
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

            MouseDown += PanAndZoomCanvas_MouseDown;
            MouseUp += PanAndZoomCanvas_MouseUp;
            MouseMove += PanAndZoomCanvas_MouseMove;
            MouseWheel += PanAndZoomCanvas_MouseWheel;

            BackgroundColor = _backgroundColor;
            // draw lines
            for (int x = -4000; x <= 4000; x += 100)
            {
                Line verticalLine = new Line
                {
                    Stroke = new SolidColorBrush(_lineColor),
                    X1 = x,
                    Y1 = -4000,
                    X2 = x,
                    Y2 = 4000
                };

                if ( x % 1000 == 0 )
                {
                    verticalLine.StrokeThickness = 6;
                }
                else
                {
                    verticalLine.StrokeThickness = 2;
                }

                Children.Add(verticalLine);
                _gridLines.Add(verticalLine);
            }

            for (int y = -4000; y <= 4000; y += 100)
            {
                Line horizontalLine = new Line
                {
                    Stroke = new SolidColorBrush(_lineColor),
                    X1 = -4000,
                    Y1 = y,
                    X2 = 4000,
                    Y2 = y
                };

                if (y % 1000 == 0)
                {
                    horizontalLine.StrokeThickness = 6;
                }
                else
                {
                    horizontalLine.StrokeThickness = 2;
                }

                Children.Add(horizontalLine);
                _gridLines.Add(horizontalLine);
            }
        }

        public const float Zoomfactor = 1.1f;

        public Color LineColor
        {
            get { return _lineColor; }

            set
            {
                _lineColor = value;

                foreach( Line line in _gridLines )
                {
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
                Background = new SolidColorBrush(_backgroundColor);
            }
        }

        public void SetGridVisibility(Visibility value)
        {
            foreach (Line line in _gridLines)
            {
                line.Visibility = value;
            }
        }

        private void PanAndZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            if (Dragging) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                e.Handled = true;
                _initialMousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Dragging = true;
                currentlyDragging = this;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                if (Children.Contains((UIElement)e.Source))
                {                    
                    _selectedElement = (UIElement)e.Source;
                    if (_selectedElement is IDraggable draggable && draggable.Draggable)
                    {
                        Point mousePosition = Mouse.GetPosition(this);
                        double x = GetLeft(_selectedElement);
                        double y = GetTop(_selectedElement);
                        Point elementPosition = new Point(x, y);
                        _draggingDelta = elementPosition - mousePosition;
                    }
                    else return;
                }
                _dragging = true;
            }
        }

        private void PanAndZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            _selectedElement = null;
            Dragging = false;
        }

        public void MoveCanvas(Vector Offset)
        {
            var translate = new TranslateTransform(Offset.X, Offset.Y);
            _transform.Matrix = translate.Value * _transform.Matrix;
            Location -= Offset;

            foreach (UIElement child in Children)
            {
                child.RenderTransform = _transform;
            }

            LocationChanged?.Invoke(this, Location);
        }
        public void MoveCanvas(Point Offset) => MoveCanvas(new Vector(Offset.X,Offset.Y));
        public void SetCanvasLocation(Point Location)
        {
            Vector delta = this.Location - Location;
            MoveCanvas(delta);
        }

        private void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Handled) return;
            if (!Dragging) return;
            if (currentlyDragging != this) return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                Point mousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
                MoveCanvas(delta);
            }

            if (_dragging && e.RightButton == MouseButtonState.Pressed)
            {
                double x = Mouse.GetPosition(this).X;
                double y = Mouse.GetPosition(this).Y;

                if (_selectedElement != null)
                {
                    SetLeft(_selectedElement, x + _draggingDelta.X);
                    SetTop(_selectedElement,  y + _draggingDelta.Y);
                }
            }
        }

        public void ZoomInverseOnce(float scaleFactor = Zoomfactor) => ZoomOnce(1f / scaleFactor);

        public void ZoomOnce(float scaleFactor = Zoomfactor)
        {
            Point mousePostion = Mouse.GetPosition(this);

            Matrix scaleMatrix = _transform.Matrix;
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
            _transform.Matrix = scaleMatrix;

            foreach (UIElement child in Children)
            {
                double x = GetLeft(child);
                double y = GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                SetLeft(child, sx);
                SetTop(child, sy);

                child.RenderTransform = _transform;
            }
        }

        private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            float scaleFactor = Zoomfactor;
            if (e.Delta < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }
            ZoomOnce(scaleFactor);
        }

        public void Reset()
        {
            foreach (UIElement child in Children)
            {
                child.RenderTransform = null;
            }
        }
    }
}
