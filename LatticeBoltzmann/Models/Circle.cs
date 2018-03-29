using System;

namespace LatticeBoltzmann.Models
{
    public class Circle: Shape
    {
        private readonly double _radius;

        public Circle(double x, double y, double r, int resolution)
            : base(x, y, resolution)
        {
            _radius = Convert.ToInt32(r * resolution);
        }

        public override bool IsSolid(int x, int y)
        {
            return Math.Sqrt(Math.Pow(X - x, 2) + Math.Pow(Y - y, 2)) <= _radius;
        }
    }
}
