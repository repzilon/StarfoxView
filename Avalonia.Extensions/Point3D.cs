using System;

namespace Avalonia.Extensions
{
	public struct Point3D : IEquatable<Point3D>
	{
		public readonly double X;
		public readonly double Y;
		public readonly double Z;

		public Point3D(double x, double y, double z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public bool Equals(Point3D other)
		{
			return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
		}

		public override bool Equals(object obj)
		{
			return obj is Point3D other && this.Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y, Z);
		}

		public static bool operator ==(Point3D left, Point3D right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Point3D left, Point3D right)
		{
			return !left.Equals(right);
		}
	}
}
