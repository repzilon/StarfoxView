using System;
using System.Collections.Generic;

namespace Avalonia.Extensions.Shapes
{
    public class DiscGeometry3D : RoundMesh3D
    {
        protected override void CalculateGeometry()
        {
            int numberOfSeparators = 4 * n + 4;

            points = new List<Point3D>(numberOfSeparators + 1);
            triangleIndices = new List<int>((numberOfSeparators + 1) * 3);

            points.Add(new Point3D(0, 0, 0));
            for (int divider = 0; divider < numberOfSeparators; divider++)
            {
                double alpha = Math.PI / 2 / (n + 1) * divider;
                points.Add(new Point3D(r * Math.Cos(alpha), 0, -1 * r * Math.Sin(alpha)));

                triangleIndices.Add(0);
                triangleIndices.Add(divider + 1);
                triangleIndices.Add(divider == numberOfSeparators - 1 ? 1 : divider + 2);
            }
        }
    }
}
