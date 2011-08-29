using System;
using System.Globalization;
using System.Windows.Data;

namespace Scorer
{
    [ValueConversion(typeof(DateTime), typeof(String))]
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime date = (DateTime)value;
            var ampm = date.Hour < 12 ? "AM" : "PM";
            return string.Format("{0:yyyy/MM/dd} {1}", date, ampm);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            var fields = strValue.Split(new[] { ' ' });

            try
            {
                DateTime resultDateTime = DateTime.Parse(fields[0], DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal);
                if (fields[1].ToUpper() == "AM")
                    return resultDateTime;
                else if (fields[1].ToUpper() == "PM")
                    return resultDateTime + new TimeSpan(12, 0, 0);
                else
                    return value;
            }
            catch
            {
                return value;
            }
        }
    }
}
