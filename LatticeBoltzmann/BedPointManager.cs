namespace LatticeBoltzmann
{
    public static class BedPointManager
    {
        public static double[,] GetBedPoints()
        {
            return new[,]
            {
                {  0.0, 0.4 },
                {  5.0, 0.9 },
                {  6.0, 0.9 },
                { 16.0, 0.3 }
            };
        }
    }
}
