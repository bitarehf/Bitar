
using System;

namespace Bitar.Helpers
{
    public static class Converters
    {
        public static DateTime UnixTimestampToDateTime(long unixTimestamp)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return epoch.AddSeconds(unixTimestamp);
        }
    }
}