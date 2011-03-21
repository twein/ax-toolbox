using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string GetAmPm(this DateTime date)
        {
            return date.TimeOfDay.TotalHours < 12 ? "AM" : "PM";
        }

        //public static TimeOfDay GetTimeOfDay(this DateTime utcDate)
        //{
        //    if (utcDate.ToLocalTime().TimeOfDay.TotalHours < 12)
        //    {
        //        return TimeOfDay.Morning;
        //    }
        //    else
        //    {
        //        return TimeOfDay.Afternoon;
        //    }
        //}
    }
}
