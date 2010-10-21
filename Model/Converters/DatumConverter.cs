using System;
using System.Globalization;
using System.Windows.Data;
using AXToolbox.Common;
using System.Collections.Generic;


namespace AXToolbox.Model.Converters
{
    [ValueConversion(typeof(Datum), typeof(String))]
    public class DatumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var datum = value as Datum;
            return datum.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            Datum resultDatum = null;

            try
            {
                resultDatum = Datum.GetInstance((string)value);
            }
            catch (KeyNotFoundException) { }

            if (resultDatum != null)
                return resultDatum;
            else
            {
                return value;
            }
        }
    }
}
