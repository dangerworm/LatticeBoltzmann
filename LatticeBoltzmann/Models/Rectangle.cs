using System;

namespace LatticeBoltzmann.Models
{
    public class Rectangle : Shape
    {
        private readonly double _width;
        private readonly double _height;

        public Rectangle(double x, double y, double w, double h, int resolution) 
            : base(x, y, resolution)
        {
            _width = Convert.ToInt32(w * resolution);
            _height = Convert.ToInt32(h * resolution);
        }

        public override bool IsSolid(int x, int y)
        {
            return Math.Abs(X - x) < _width / 2 && Math.Abs(Y - y) < _height / 2;
        }
    }
}
