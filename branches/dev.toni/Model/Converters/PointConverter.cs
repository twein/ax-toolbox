using System;
using System.Globalization;
using System.Windows.Data;
using AXToolbox.Common;


namespace AXToolbox.Model.Converters
{
    [ValueConversion(typeof(UTMPoint), typeof(String))]
    public class PointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UTMPoint point = value as UTMPoint;
            return point.ToString(PointData.UTMCoords);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            UTMPoint resultPoint;
            if (UTMPoint.TryParse(strValue, out resultPoint))
                return resultPoint;
            else
            {
                return value;
            }
        }
    }
}
