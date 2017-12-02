using System;

namespace LatticeBoltzmann.Models
{
    public class Circle: Shape
    {
        private double _r { get; }

        public Circle(double x, double y, double r)
            : base(x, y)
        {
            _r = r;
        }

        public override bool IsSolid(double x, double y)
        {
            return Math.Sqrt(Math.Pow(x - X, 2) + Math.Pow(y - Y / 2, 2)) <= _r;
        }
    }
}
