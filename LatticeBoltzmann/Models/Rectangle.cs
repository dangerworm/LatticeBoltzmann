using System;

namespace LatticeBoltzmann.Models
{
    public class Rectangle : Shape
    {
        private readonly double _width;
        private readonly double _height;

        public Rectangle(double x, double y, double w, double h) 
            : base(x, y)
        {
            _width = w;
            _height = h;
        }

        public override bool IsSolid(double x, double y)
        {
            return Math.Abs(x - X) < _width / 2 && Math.Abs(y - Y) < _height / 2;
        }
    }
}
