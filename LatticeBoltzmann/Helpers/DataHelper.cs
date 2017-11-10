namespace LatticeBoltzmann.Helpers
{
    public static class DataHelper
    {
        public static T[,] GetNew2DArray<T>(int x, int y, T initialValue)
        {
            var nums = new T[x, y];
            for (var i = 0; i < x * y; i++)
            {
                nums[i % x, i / x] = initialValue;
            }
            return nums;
        }
    }
}
