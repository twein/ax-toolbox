using System;
using System.Globalization;
using System.Windows.Data;
using AXToolbox.Common;


namespace AXToolbox.Model.Converters
{
    [ValueConversion(typeof(Point), typeof(String))]
    public class PointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point point = value as Point;
            return point.ToString(PointInfo.Datum | PointInfo.UTMCoords).TrimEnd();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            Point resultPoint;
            if (Point.TryParse(strValue, out resultPoint))
                return resultPoint;
            else
            {
                return value;
            }
        }
    }
}
