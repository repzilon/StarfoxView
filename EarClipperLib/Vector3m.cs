using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace EarClipperLib
{
    public class Vector3m : ICloneable
    {
        internal DynamicProperties DynamicProperties = new DynamicProperties();

        public Vector3m(Rational x, Rational y, Rational z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3m(Vector3m v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public static Vector3m Zero()
        {
            return new Vector3m(0, 0, 0);
        }

        public Rational X { get; set; }

        public Vector3m Absolute()
        {
            return new Vector3m(X.AbsoluteValue, Y.AbsoluteValue, Z.AbsoluteValue);
        }

        public Rational Y { get; set; }
        public Rational Z { get; set; }

        public object Clone()
        {
            return new Vector3m(X, Y, Z);
        }

        public void ImplizitNegated()
        {
            X = -X; Y = -Y; Z = -Z;
        }

        public Vector3m Negated()
        {
            return new Vector3m(-X, -Y, -Z);
        }

        public Vector3m Plus(Vector3m a)
        {
            return new Vector3m(X + a.X, Y + a.Y, Z + a.Z);
        }

        public Vector3m Minus(Vector3m a)
        {
            return new Vector3m(X - a.X, Y - a.Y, Z - a.Z);
        }

        public Vector3m Times(Rational a)
        {
            return new Vector3m(X * a, Y * a, Z * a);
        }

        public Vector3m DividedBy(Rational a)
        {
            return new Vector3m(X / a, Y / a, Z / a);
        }

        public Rational Dot(Vector3m a)
        {
            return X * a.X + Y * a.Y + Z * a.Z;
        }

        public Vector3m Lerp(Vector3m a, Rational t)
        {
            return Plus(a.Minus(this).Times(t));
        }

        public double Length()
        {
            return Math.Sqrt(Dot(this).ToDouble());
        }

        public Rational LengthSquared()
        {
            return Dot(this);
        }

        public Vector3m ShortenByLargestComponent()
        {
            if (LengthSquared() == 0)
                return new Vector3m(0, 0, 0);
            var absNormal = Absolute();
            Rational largestValue = 0;
            if (absNormal.X >= absNormal.Y && absNormal.X >= absNormal.Z)
                largestValue = absNormal.X;
            else if (absNormal.Y >= absNormal.X && absNormal.Y >= absNormal.Z)
                largestValue = absNormal.Y;
            else
            {
                largestValue = absNormal.Z;
            }
            Debug.Assert(largestValue != 0);
            return this / largestValue;
        }

        public Vector3m Cross(Vector3m a)
        {
            return new Vector3m(
            Y * a.Z - Z * a.Y,
            Z * a.X - X * a.Z,
            X * a.Y - Y * a.X
            );
        }

        internal bool IsZero()
        {
            return X == 0 && Y == 0 && Z == 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Vector3m;

            if (other == null)
            {
                return false;
            }

            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public static Vector3m operator +(Vector3m a, Vector3m b)
        {
            return a.Plus(b);
        }

        public static Vector3m operator -(Vector3m a, Vector3m b)
        {
            return a.Minus(b);
        }

        public static Vector3m operator *(Vector3m a, Rational d)
        {
            return new Vector3m(a.X * d, a.Y * d, a.Z * d);
        }

        public static Vector3m operator /(Vector3m a, Rational d)
        {
            return a.DividedBy(d);
        }

        public override string ToString()
        {
            return "Vector:" + " " + X.ToDouble() + " " + Y.ToDouble() + " " + Z.ToDouble() + " ";
        }

        public static Vector3m PlaneNormal(Vector3m v0, Vector3m v1, Vector3m v2)
        {
            var a = v1 - v0;
            var b = v2 - v0;
            return a.Cross(b);
        }

        public bool SameDirection(Vector3m he)
        {
            var res = Cross(he);
            return res.X == 0 && res.Y == 0 && res.Z == 0;
        }
    }
}
