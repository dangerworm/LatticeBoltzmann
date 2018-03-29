using System;

namespace LatticeBoltzmann.Models
{
    public abstract class Shape
    {
        protected int X;
        protected int Y;

        protected Shape(double x, double y, int resolution)
        {
            X = Convert.ToInt32(x * resolution);
            Y = Convert.ToInt32(y * resolution);
        }

        public abstract bool IsSolid(int x, int y);
    }
}
