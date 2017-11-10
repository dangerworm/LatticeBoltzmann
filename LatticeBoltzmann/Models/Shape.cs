namespace LatticeBoltzmann.Models
{
    public abstract class Shape
    {
        protected double X;
        protected double Y;

        protected Shape(double x, double y)
        {
            X = x;
            Y = y;
        }

        public abstract bool IsSolid(double x, double y, double r);
    }
}
