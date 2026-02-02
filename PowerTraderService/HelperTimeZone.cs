using System;

namespace PowerTrader
{
    internal static class HelperTimeZone
    {
        private static readonly TimeZoneInfo LondonTimeZone = GetLondonTimeZone();

        public static DateTime GetLondonNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LondonTimeZone);
        }

        private static TimeZoneInfo GetLondonTimeZone()
        {

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            }
        }
    }
}
