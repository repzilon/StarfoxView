using System.Collections.Generic;

namespace Avalonia.Extensions.Shapes
{
    public abstract class RoundMesh3D
    {
        protected int n = 10;
        protected int r = 20;
        protected List<Point3D> points;
        protected List<int> triangleIndices;

        public virtual int Radius
        {
            get { return r; }
            set { r = value; CalculateGeometry(); }
        }

        public virtual int Separators
        {
            get { return n; }
            set { n = value; CalculateGeometry(); }
        }

        public IList<Point3D> Points
        {
            get { return points; }
        }

        public IList<int> TriangleIndices
        {
            get { return triangleIndices; }
        }

        protected abstract void CalculateGeometry();
    }
}
