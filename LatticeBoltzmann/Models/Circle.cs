using System;

namespace LatticeBoltzmann.Models
{
    public class Circle: Shape
    {
        public Circle(double x, double y)
            : base(x, y)
        {
        }

        public override bool IsSolid(double x, double y, double r)
        {
            return Math.Sqrt(Math.Pow(x - X, 2) + Math.Pow(y - Y / 2, 2)) <= r;
        }
    }
}
