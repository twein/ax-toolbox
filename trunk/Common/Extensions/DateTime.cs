using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string AmPm(this DateTime date)
        {
            return date.TimeOfDay.TotalHours < 12 ? "AM" : "PM";
        }

        public static DateTime DateAmPm(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, (date.TimeOfDay.TotalHours < 12 ? 0 : 12), 0, 0);
        }
    }
}
