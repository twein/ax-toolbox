using System;
using System.Globalization;
using System.Windows.Data;


namespace AXToolbox.Model.Converters
{
    [ValueConversion(typeof(DateTime), typeof(String))]
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime date = (DateTime)value;
            return string.Format("{0:yyyy/MM/dd}", date);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal, out resultDateTime))
            {
                return resultDateTime;
            }
            else
            {
                return value;
            }
        }

        [ValueConversion(typeof(DateTime), typeof(String))]
        public class DateTimeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                DateTime date = (DateTime)value;
                return string.Format("{0:yyyy/MMM/dd HH:mm}", date);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                string strValue = value as string;
                DateTime resultDateTime;
                if (DateTime.TryParse(strValue, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal, out resultDateTime))
                {
                    return resultDateTime;
                }
                else
                {
                    return value;
                }
            }
        }
    }
}
