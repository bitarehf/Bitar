using System;

namespace Bitar.Helpers
{
    public class MathDecimals
    {
        public static int GetDecimals(decimal d, int i = 0)
        {
            decimal multiplied = (decimal) ((double) d * Math.Pow(10, i));
            if (Math.Round(multiplied) == multiplied)
                return i;
            return GetDecimals(d, i + 1);
        }
    }
}