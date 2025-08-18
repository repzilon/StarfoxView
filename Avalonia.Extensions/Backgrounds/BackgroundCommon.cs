using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Extensions.Backgrounds
{
	internal static class BackgroundCommon
	{
		private static readonly IReadOnlyList<Vector3D> Normals = new List<Vector3D>() {
			new Vector3D(0, 0, 1),
			new Vector3D(0, 0, 1),
			new Vector3D(0, 0, 1),
			new Vector3D(0, 0, 1),
			new Vector3D(0, 0, 1),
			new Vector3D(0, 0, 1)
		};

		internal static IReadOnlyList<Point3D> GeneratePositions(float pos)
		{
			return new List<Point3D>() {
				new Point3D(-pos, -pos, pos),
				new Point3D(pos, -pos, pos),
				new Point3D(pos, pos, pos),
				new Point3D(pos, pos, pos),
				new Point3D(-pos, pos, pos),
				new Point3D(-pos, -pos, pos)
			};
		}

		private static IReadOnlyList<int> Triangles = new List<int>() { 0, 1, 2, 3, 4, 5 };

		/*
		internal static ModelVisual3D GenerateBillboard(Point3D position, Color color,
		IReadOnlyList<Point3D> positionList)
		{
			return new ModelVisual3D() {
				Content = new GeometryModel3D() {
					Geometry = new MeshGeometry3D() {
						Normals            = Normals,
						Positions          = positionList,
						TextureCoordinates = new List<Point>() { new Point(0, 0), new Point(1, 1) },
						TriangleIndices    = Triangles
					},
					Material = new DiffuseMaterial() {
						Brush = new SolidColorBrush(color)
					}
				},
				Transform = new Transform3DGroup() {
					Children = new Transform3DCollection() {
						new TranslateTransform3D() {
							OffsetX = position.X,
							OffsetY = position.Y,
							OffsetZ = position.Z
						},
					}
				}
			};
		} // */

		internal static void LoadScene_Confetti(double depth, double linearRange, double stars, Random rand,
		IAddChild spaceScene, IReadOnlyList<Point3D> Positions, params Color[] Palette)
		{
			var positions = new List<Point3D>();
			int palette   = 0;
			for (int star = 0; star < stars; star++) {
				if (palette == Palette.Length) {
					palette = 0;
				}

				Color   color = Palette[palette];
				Point3D position;
				if (star < (stars / 2) + 1) {
					position = new Point3D((rand.NextDouble() * linearRange) - (linearRange / 2),
						rand.NextDouble() * -linearRange, rand.NextDouble() * -depth);
					positions.Add(position);
				} else {
					var idx = (star - 1) - (int)(stars / 2);
					position       = positions[idx];
					position       = new Point3D(position.X, position.Y + (linearRange / 2), position.Z);
					positions[idx] = position;
				}

				// TODO : make type of spaceScene more specific
				// TODO : Viewport3D and related 3D APIs are not yet supported
				// https://docs.avaloniaui.net/xpf/missing-features
				//spaceScene.AddChild(GenerateBillboard(position, color, Positions));
				palette++;
			}
		}

		internal static void LoadScene_Space(double depth, double linearRange, double stars, Random rand,
		IAddChild spaceScene, IReadOnlyList<Point3D> Positions,  params Color[] Palette)
		{
			var positions = new List<Point3D>();
			int palette   = 0;
			for (int star = 0; star < stars; star++) {
				if (palette == Palette.Length) {
					palette = 0;
				}

				Color   color = Palette[palette];
				Point3D position;
				if (star < (stars / 2) + 1) {
					position = new Point3D((rand.NextDouble() * linearRange) - (linearRange / 2),
						(rand.NextDouble() * linearRange) - (linearRange / 2), rand.NextDouble() * -depth);
					positions.Add(position);
				} else {
					var idx = (star - 1) - (int)(stars / 2);
					position       = positions[idx];
					position       = new Point3D(position.X, position.Y, position.Z + (depth / 2));
					positions[idx] = position;
				}

				//spaceScene.AddChild(GenerateBillboard(position, color, Positions));
				palette++;
			}
		}
	}
}
