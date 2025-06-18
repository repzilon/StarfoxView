using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.UI.Extensions.Backgrounds
{
    /// <summary>
    /// Interaction logic for SpacePanel.xaml
    /// </summary>
    public partial class ConfettiParticlesPanel : UserControl
    {
        const double DEPTH = 100, LINEAR_RANGE = 70, STARS = 500;

        static readonly Random Rand = new Random();

        static readonly Vector3DCollection Normals = new Vector3DCollection()
        {
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1)
        };
        const float POS = .07f;
        static readonly Point3DCollection Positions = new Point3DCollection()
        {
            new Point3D((float)-POS, (float)-POS, (float)POS),
            new Point3D((float)POS, (float)-POS, (float)POS),
            new Point3D((float)POS, (float)POS, (float)POS),
            new Point3D((float)POS, (float)POS, (float)POS),
            new Point3D((float)-POS, (float)POS, (float)POS),
            new Point3D((float)-POS, (float)-POS, (float)POS)
        };
        static readonly Int32Collection Triangles = new Int32Collection() { 0, 1, 2, 3, 4, 5 };

        public ConfettiParticlesPanel()
        {
            InitializeComponent();

            LoadScene(Colors.Blue, Colors.Red, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.White, Colors.Green);
        }

        private void LoadScene(params Color[] Palette)
        {
            var positions = new List<Point3D>();
            int palette = 0;
            for (int star = 0; star < STARS; star++)
            {
                if (palette == Palette.Length)
                    palette = 0;
                Color color = Palette[palette];
                var position = new Point3D();
                if (star < (STARS / 2) + 1)
                {
                    position.X = (Rand.NextDouble() * LINEAR_RANGE) - (LINEAR_RANGE / 2);
                    position.Y = Rand.NextDouble() * -LINEAR_RANGE;
                    position.Z = (Rand.NextDouble() * -DEPTH);
                    positions.Add(position);
                }
                else
                {
                    position = positions[(star - 1) - (int)(STARS / 2)];
                    position.Y += LINEAR_RANGE / 2;
                }
                SpaceScene.Children.Add(GenerateBillboard(position, color));
                palette++;
            }
        }

        private static ModelVisual3D GenerateBillboard(Point3D Position, Color color)
        {
            return new ModelVisual3D()
            {
                Content = new GeometryModel3D()
                {
                    Geometry = new MeshGeometry3D()
                    {
                        Normals = Normals,
                        Positions = Positions,
                        TextureCoordinates = new PointCollection() { new Point(0, 0), new Point(1, 1) },
                        TriangleIndices = Triangles
                    },
                    Material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(color)
                    }
                },
                Transform = new Transform3DGroup()
                {
                    Children = new Transform3DCollection()
                    {
                        new TranslateTransform3D()
                        {
                            OffsetX = Position.X,
                            OffsetY = Position.Y,
                            OffsetZ = Position.Z
                        },
                    }
                }
            };
        }
    }
}
