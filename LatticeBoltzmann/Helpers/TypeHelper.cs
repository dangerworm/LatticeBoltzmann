using System;
using System.ComponentModel;

namespace LatticeBoltzmann.Helpers
{
    public static class TypeHelper
    {
        public static T Convert<T>(string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static Type GetTypeFromString(this string value)
        {
            int intResult;
            if (int.TryParse(value, out intResult))
            {
                return typeof(int);
            }

            double doubleResult;
            if (double.TryParse(value, out doubleResult))
            {
                return typeof(double);
            }

            return typeof(string);
        }
    }
}
