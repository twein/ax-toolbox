using System;
using System.Globalization;
using System.Windows.Data;

namespace AXToolbox.Model.Converters
{
    public class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertValue(value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertValue(value);
        }
        private static object ConvertValue(object value)
        {
            if (!(value is bool))
            {
                throw new NotSupportedException("Only bool is supported.");
            }
            return !(bool)value;
        }
    }
}
