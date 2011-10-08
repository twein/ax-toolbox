using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AXToolbox.GpsLoggers
{
    [Serializable]
    public class GoalDeclaration : ITime
    {
        public enum DeclarationType { GoalName, CompetitionCoordinates };

        public DeclarationType Type { get; protected set; }
        public int Number { get; protected set; }
        public DateTime Time { get; protected set; }
        public string Name { get; protected set; }
        public double Easting4Digits { get; protected set; }
        public double Northing4Digits { get; protected set; }
        public double Altitude { get; set; }
        public string Description { get; set; }

        public GoalDeclaration(int number, DateTime time, string definition, double altitude)
        {
            Number = number;
            Time = time;
            Altitude = altitude;

            if (definition.Length == 9 && definition[4] == '/')
            {
                // type 0000/0000
                Type = DeclarationType.CompetitionCoordinates;
                var coords = definition.Split(new char[] { '/' });
                Easting4Digits = double.Parse(coords[0]);
                Northing4Digits = double.Parse(coords[1]);
            }
            else
            {
                //Type freeform
                Type = DeclarationType.GoalName;
                Name = definition.TrimEnd(new char[] { '/' });
            }

        }

        public override string ToString()
        {
            return ToString(AXPointInfo.Name | AXPointInfo.Time | AXPointInfo.Declaration | AXPointInfo.AltitudeFeet);
        }
        public string ToString(AXPointInfo info)
        {
            var str = new StringBuilder();

            if ((info & AXPointInfo.Name) > 0)
                str.Append(string.Format("{0:00} ", Number));

            if ((info & AXPointInfo.Date) > 0)
                str.Append(Time.ToLocalTime().ToString("yyyy/MM/dd "));

            if ((info & AXPointInfo.Time) > 0)
                str.Append(Time.ToLocalTime().ToString("HH:mm:ss "));

            if ((info & AXPointInfo.Declaration) > 0)
                if (Type == DeclarationType.GoalName)
                    str.Append(Name + " ");
                else
                    str.Append(string.Format("{0:0000}/{1:0000} ", Easting4Digits, Northing4Digits));

            if ((info & AXPointInfo.AltitudeFeet) > 0)
                if (double.IsNaN(Altitude))
                    str.Append("- ");
                else
                    str.Append(Altitude.ToString("0 "));

            if ((info & AXPointInfo.AltitudeMeters) > 0)
                if (double.IsNaN(Altitude))
                    str.Append("- ");
                else
                    str.Append(Altitude.ToString("0m "));

            return str.ToString();
        }

        public static GoalDeclaration Parse(string strValue)
        {
            var fields = strValue.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var number = int.Parse(fields[0]);
            var time = DateTime.Parse(fields[1] + ' ' + fields[2], DateTimeFormatInfo.InvariantInfo).ToUniversalTime();
            var definition = fields[3];
            double altitude = 0;
            if (fields[4] == "-")
                altitude = double.NaN;
            else
                altitude = double.Parse(fields[4], NumberFormatInfo.InvariantInfo);

            return new GoalDeclaration(number, time, definition, altitude);
        }
    }

    [ValueConversion(typeof(GoalDeclaration), typeof(String))]
    public class GoalDeclarationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var declaration = value as GoalDeclaration;
            return declaration.ToString(AXPointInfo.Name | AXPointInfo.Date | AXPointInfo.Time | AXPointInfo.Declaration | AXPointInfo.AltitudeFeet);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return GoalDeclaration.Parse((string)value);
            }
            catch
            {
                return value;
            }
        }
    }
}
