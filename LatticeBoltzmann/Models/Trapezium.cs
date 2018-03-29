using System;

namespace LatticeBoltzmann.Models
{
    public class Trapezium : Shape
    {
        private const int TOP_LEFT = 0;
        private const int TOP_RIGHT = 1;
        private const int BOTTOM_RIGHT = 2;
        private const int BOTTOM_LEFT = 3;

        private const int X_INDEX = 0;
        private const int Y_INDEX = 1;

        private readonly int[,] _points;

        public Trapezium(double[,] points, int resolution) 
            : base(FindCentre(points)[X_INDEX], FindCentre(points)[Y_INDEX], resolution)
        {
            int xMax = points.GetLength(0), yMax = points.GetLength(1);
            _points = new int[xMax, yMax];

            for (var y = 0; y < yMax; y++)
            {
                for (var x = 0; x < xMax; x++)
                {
                    _points[x, y] = Convert.ToInt32(_points[x, y] * resolution);
                }
            }
        }

        public override bool IsSolid(int x, int y)
        {
            // Not inside top and bottom parallel lines
            if (!(y <= _points[TOP_LEFT, Y_INDEX]) || !(y >= _points[BOTTOM_LEFT, Y_INDEX]))
            {
                return false;
            }

            // Inside inner rectangle
            if (x >= _points[TOP_LEFT, X_INDEX] &&
                x >= _points[BOTTOM_LEFT, X_INDEX] &&
                x <= _points[TOP_RIGHT, X_INDEX] &&
                x <= _points[BOTTOM_RIGHT, X_INDEX])
            {
                return true;
            }

            var borderLeft = _points[BOTTOM_LEFT, X_INDEX] + ((y - _points[BOTTOM_LEFT, Y_INDEX]) * ((_points[TOP_LEFT, X_INDEX] - _points[BOTTOM_LEFT, X_INDEX]) / (_points[TOP_LEFT, Y_INDEX] - _points[BOTTOM_LEFT, Y_INDEX])));
            var borderRight = _points[BOTTOM_RIGHT, X_INDEX] + ((y - _points[BOTTOM_RIGHT, Y_INDEX]) * ((_points[TOP_RIGHT, X_INDEX] - _points[BOTTOM_RIGHT, X_INDEX]) / (_points[TOP_RIGHT, Y_INDEX] - _points[BOTTOM_RIGHT, Y_INDEX])));

            return x >= borderLeft && x <= borderRight;
        }

        public static double[] FindCentre(double[,] points)
        {
            var topLength = points[TOP_RIGHT, X_INDEX] - points[TOP_LEFT, X_INDEX];
            var bottomLength = points[BOTTOM_RIGHT, X_INDEX] - points[BOTTOM_LEFT, X_INDEX];
            var height = Math.Abs(points[TOP_LEFT, Y_INDEX] - points[BOTTOM_LEFT, Y_INDEX]);

            var x = topLength + bottomLength / 4;
            var y = (height / 3) * (bottomLength + (2 * topLength)) / (topLength + bottomLength);

            return new[] {x, y};
        }
    }
}
