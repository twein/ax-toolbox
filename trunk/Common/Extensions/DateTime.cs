using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string GetAmPm(this DateTime utcDate)
        {
            return utcDate.ToLocalTime().TimeOfDay.TotalHours < 12 ? "AM" : "PM";
        }

        public static DateTime ToDateAmPm(this DateTime utcDate)
        {
            var date = utcDate.ToLocalTime();
            return new DateTime(date.Year, date.Month, date.Day, (date.TimeOfDay.TotalHours < 12 ? 0 : 12), 0, 0);
        }

        public static DateTime StripTimePart(this DateTime utcDate)
        {
            var date = utcDate.ToLocalTime();
            return new DateTime(date.Year, date.Month, date.Day);
        }
    }
}
