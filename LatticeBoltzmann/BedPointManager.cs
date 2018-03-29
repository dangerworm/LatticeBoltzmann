namespace LatticeBoltzmann
{
    public static class BedPointManager
    {
        public static double[,] GetBedPoints()
        {
            //return new[,]
            //{
            //    {  0.0, 0.4 },
            //    {  5.0, 0.9 },
            //    {  6.0, 0.9 },
            //    { 16.0, 0.3 }
            //};

            return new[,]
            {
                {  0.0, 0.20 },
                {  2.0, 1.50 },
                {  4.0, 1.70 },
                {  6.0, 0.80 },
                {  8.0, 0.30 },
                { 10.0, 0.80 },
                { 12.0, 1.70 },
                { 14.0, 1.50 },
                { 16.0, 0.20 }
            };
        }
    }
}
