using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string AmPm(this DateTime date)
        {
            return date.TimeOfDay.TotalHours < 12 ? "AM" : "PM";
        }
    }
}
