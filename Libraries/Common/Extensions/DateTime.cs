using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string GetDateAmPm(this DateTime date)
        {
            return date.Date.ToShortDateString() + " " + ((date.TimeOfDay.TotalHours < 12) ? "AM" : "PM");
        }

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
