using System;
using System.Collections.Generic;
using System.Drawing;

namespace LatticeBoltzmann.Models
{
    public class Particle
    {
        public Point CurrentPosition;
        public Color Colour;

        public IList<Point> History;

        public Particle(int x, int y, Color colour)
        {
            CurrentPosition = new Point(x, y);
            Colour = colour;

            History = new List<Point> {new Point(x, y)};
        }

        public void Move(double dx, double dy)
        {
            Move(Convert.ToInt32(dx), Convert.ToInt32(dy));
        }

        public void Move(Point vector)
        {
            Move(vector.X, vector.Y);
        }

        public void Move(int dx, int dy)
        {
            CurrentPosition.Offset(dx, dy);

            History.Add(new Point(CurrentPosition.X, CurrentPosition.Y));
        }
    }
}
