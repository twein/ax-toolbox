using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace AXToolbox.Common
{
    public static class Parsers
    {
        public static int ParseInt(string str)
        {
            return int.Parse(str, NumberFormatInfo.InvariantInfo);
        }
        public static double ParseDouble(string str)
        {
            return double.Parse(str, NumberFormatInfo.InvariantInfo);
        }
        public static double ParseLengthOrNaN(string str)
        {
            str = str.Trim();
            if (str == "" || str == "-")
                return double.NaN;
            else
                return ParseLength(str);
        }
        public static double ParseLength(string str)
        {
            double length = 0;

            str = str.Trim().ToLower();
            var regex = new Regex(@"(?<value>[\d\.]+)\s*(?<units>\w*)");
            var matches = regex.Matches(str);
            if (matches.Count != 1)
            {
                throw new ArgumentException("Syntax error in distance or altitude definition: " + str);
            }
            else
            {
                length = ParseDouble(matches[0].Groups["value"].Value);
                var units = matches[0].Groups["units"].Value;
                switch (units)
                {
                    case "m":
                        break;
                    case "km":
                        length *= 1000;
                        break;
                    case "ft":
                        length *= 0.3048;
                        break;
                    case "mi":
                        length *= 1609.344;
                        break;
                    case "nm":
                        length *= 1852;
                        break;
                    default:
                        throw new ArgumentException("Syntax error in distance or altitude definition: " + str);
                }
            }

            return length;
        }
        public static DateTime ParseLocalDatetime(string str)
        {
            return DateTime.Parse(str, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
        }
        public static TimeSpan ParseTimeSpan(string str)
        {
            return TimeSpan.Parse(str, DateTimeFormatInfo.InvariantInfo);
        }
        public static string ParseString(string str)
        {
            return str.Trim(new char[] { '"', '\'' });
        }
        public static bool ParseBoolean(string str)
        {
            bool value = false;
            switch (str.ToLower())
            {
                case "true":
                    value = true;
                    break;
                case "false":
                    value = false;
                    break;
                default:
                    throw new ArgumentException("Syntax error in boolean definition: " + str);
            }

            return value;
        }
        public static Color ParseColor(string str)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(str);
            }
            catch
            {
                throw new ArgumentException("Unknown color: " + str);
            }
        }
    }
}
