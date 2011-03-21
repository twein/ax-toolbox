using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AXToolbox.Model.Converters
{
    public class BoolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return new SolidColorBrush(Colors.Red);
            else
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF707070"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
