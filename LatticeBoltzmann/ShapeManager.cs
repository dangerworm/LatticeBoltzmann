using System.Collections.Generic;
using System.Collections.ObjectModel;
using LatticeBoltzmann.Models;
using Rectangle = LatticeBoltzmann.Models.Rectangle;

namespace LatticeBoltzmann
{
    public static class ShapeManager
    {
        public static IEnumerable<Shape> GetColumns(int resolution)
        {
            var shapes = new Collection<Shape>
            {
                new Circle(8.0, 4.0, 1.5, resolution),
                new Circle(8.0, 8.0, 1.5, resolution),
                new Circle(8.0, 12.0, 1.5, resolution)
            };

            foreach (var shape in shapes)
            {
                yield return shape;
            }
        }

        public static IEnumerable<Shape> Get5Columns(int resolution)
        {
            var shapes = new Collection<Shape>
            {
                new Circle(8.0, 2.0, 0.5, resolution),
                new Circle(8.0, 5.0, 0.5, resolution),
                new Circle(8.0, 8.0, 0.5, resolution),
                new Circle(8.0, 11.0, 0.5, resolution),
                new Circle(8.0, 14.0, 0.5, resolution)
            };

            foreach (var shape in shapes)
            {
                yield return shape;
            }
        }

        public static IEnumerable<Shape> GetBarriers(int resolution)
        {
            // Top left, top right, bottom left, bottom right
            var trapezium1Points = new[,]
            {
                { 5.0, 5.0 },
                { 7.0, 5.0 },
                { 7.0, 4.0 },
                { 5.0, 4.0 }
            };

            var trapezium2Points = new[,]
            {
                { 5.0, 12.0 },
                { 7.0, 12.0 },
                { 7.0, 11.0 },
                { 5.0, 11.0 }
            };

            var shapes = new Collection<Shape>
            {
                new Rectangle(5.0, 4.5, 1, 1, resolution),
                new Rectangle(5.0, 11.5, 1, 1, resolution),
                new Rectangle(7.0, 4.5, 1, 1, resolution),
                new Rectangle(7.0, 11.5, 1, 1, resolution),
                new Trapezium(trapezium1Points, resolution),
                new Trapezium(trapezium2Points, resolution)
            };

            foreach (var shape in shapes)
            {
                yield return shape;
            }
        }
    }
}
