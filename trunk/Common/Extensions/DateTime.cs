﻿using System;

namespace AXToolbox.Common
{
    public static class DateTimeExtensions
    {
        public static string GetAmPm(this DateTime utcDate)
        {
            return utcDate.ToLocalTime().TimeOfDay.TotalHours < 12 ? "AM" : "PM";
        }

        public static TimeOfDay GetTimeOfDay(this DateTime utcDate)
        {
            if (utcDate.ToLocalTime().TimeOfDay.TotalHours < 12)
            {
                return TimeOfDay.Morning;
            }
            else
            {
                return TimeOfDay.Afternoon;
            }
        }

        public static DateTime StripTimePart(this DateTime utcDate)
        {
            var date = utcDate.ToLocalTime();
            return new DateTime(date.Year, date.Month, date.Day);
        }
    }
}
