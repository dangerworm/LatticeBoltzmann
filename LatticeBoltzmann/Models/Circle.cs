using System;

namespace LatticeBoltzmann.Models
{
    public class Circle: Shape
    {
        private readonly double _radius;

        public Circle(double x, double y, double r)
            : base(x, y)
        {
            _radius = r;
        }

        public override bool IsSolid(double x, double y)
        {
            return Math.Sqrt(Math.Pow(x - X, 2) + Math.Pow(y - Y, 2)) <= _radius;
        }
    }
}
